using Amazon.Runtime;
using Amazon.SQS.Model;
using Palantiri.Shared.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using Palantiri.Shared.Amazon.SQS;
using Amazon.S3;
using Amazon.S3.Model;
using Palantiri.Shared.Observability.TraceContext;

namespace Palantiri.Shared.Amazon.S3
{
    public class ReadRepository(IOptions<AmazonS3Options> options, ILoggerFactory logger) : IReadRepository
    {

        private readonly AmazonS3Options _options = options.Value;

        private readonly AmazonS3Client _amazonS3 = new AmazonS3Client(
                new BasicAWSCredentials(options.Value.AccessKey, options.Value.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = options.Value.ServiceUrl,
                });

        private readonly ILogger _logger = logger.CreateLogger<ReadRepository>();

        private static readonly ActivitySource _activitySource = new(nameof(MessagePublisher));
        private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

        public async Task<(Stream, string, string)> ReadAsync(string path)
        {

            using var activity = _activitySource.StartActivity("AWS:S3:Read", ActivityKind.Producer);

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }
            var props = new Dictionary<string, MessageAttributeValue>();
            // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.

            var request = new GetObjectRequest()
            {
                BucketName = _options.Buckets["Publisher"],
                Key = path
            };
            try
            {
                var response = await _amazonS3.GetObjectAsync(request);

                var parentContext = _propagator.Extract(default, response.Metadata, AmazonTraceContext.ExtractTraceContextFromBasicProperties);

                activity?.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);

                using (var activityWithLinks = _activitySource.StartActivity("AWS:S3:Readed", ActivityKind.Consumer, parentContext.ActivityContext, links: new List<ActivityLink>() { new(activity!.Context) }))
                {
                    activity.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);
                    activityWithLinks?.AddEvent(new ActivityEvent("Message received"));

                    activityWithLinks?.AddTag("bucket-name", response.BucketName);
                    activityWithLinks?.AddTag("file-path", path);

                    activityWithLinks?.SetStatus(response.HttpStatusCode == System.Net.HttpStatusCode.OK ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                }
                return new (response.ResponseStream, response.Headers.ContentType, path);
            }
            catch (Exception e)
            {
                _logger.LogError("{e}", e);
                throw;
            }
        }
    }
}   
