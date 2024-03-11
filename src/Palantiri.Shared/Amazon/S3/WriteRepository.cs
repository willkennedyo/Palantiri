using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3;
using Amazon.SQS.Model;
using Palantiri.Shared.Amazon.SQS;
using Palantiri.Shared.SQS;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Palantiri.Shared.Observability.TraceContext;

namespace Palantiri.Shared.Amazon.S3
{
    public class WriteRepository(IOptions<AmazonOptions> options, ILoggerFactory logger) : IWriteRepository
    {

        private readonly AmazonOptions _options = options.Value;

        private readonly AmazonS3Client _amazonS3 = new(
                new BasicAWSCredentials(options.Value.AccessKey, options.Value.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = options.Value.S3.ServiceUrl
                });

        private readonly ILogger _logger = logger.CreateLogger<WriteRepository>();

        private static readonly ActivitySource _activitySource = new(nameof(MessagePublisher));
        private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

        public async Task WriteAsync(Stream stream, string type, string path)
        {

            using var activity = _activitySource.StartActivity("AWS:S3:Write", ActivityKind.Producer);

            // Depending on Sampling (and whether a listener is registered or not), the
            // activity above may not be created.
            // If it is created, then propagate its context.
            // If it is not created, the propagate the Current context,
            // if any.
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
            _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props, AmazonTraceContext.InjectTraceContextIntoBasicProperties);

            var request = new PutObjectRequest()
            {
                InputStream = stream,
                BucketName = _options.S3.Buckets["Publisher"],
                Key = path,
                ContentType = type
            };
            props.ToList().ForEach(_ => request.Metadata.Add(_.Key, _.Value.StringValue));
            try
            {
                var response = await _amazonS3.PutObjectAsync(request);
                activity.SetStatus(response.HttpStatusCode == HttpStatusCode.OK ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            }
            catch (Exception e)
            {
                _logger.LogError("{e}", e);
            }
        }

        public async Task DeleteAsync(string filePath)
        {
            using var activity = _activitySource.StartActivity("AWS:S3:Write", ActivityKind.Producer);

            // Depending on Sampling (and whether a listener is registered or not), the
            // activity above may not be created.
            // If it is created, then propagate its context.
            // If it is not created, the propagate the Current context,
            // if any.
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
            try
            {
                DeleteObjectResponse response = await _amazonS3.DeleteObjectAsync(_options.S3.Buckets["Publisher"], filePath);

                activity.AddTag("file-path", filePath);
                activity.SetStatus(response.HttpStatusCode == HttpStatusCode.OK ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                    throw new Exception($"Error deleting file. HttpStatusCode: {response.HttpStatusCode}");
            }
            catch (Exception e)
            {
                _logger.LogError("{e}", e);
            }
        }
    }
}
