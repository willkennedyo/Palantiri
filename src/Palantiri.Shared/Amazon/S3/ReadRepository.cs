using Amazon.Runtime;
using Palantiri.Shared.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Palantiri.Shared.Amazon.SQS;
using Amazon.S3;
using Amazon.S3.Model;

namespace Palantiri.Shared.Amazon.S3
{
    public class ReadRepository(IOptions<AmazonOptions> options, ILoggerFactory logger) : IReadRepository
    {

        private readonly AmazonOptions _options = options.Value;

        private readonly AmazonS3Client _amazonS3 = new AmazonS3Client(
                new BasicAWSCredentials(options.Value.AccessKey, options.Value.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = options.Value.S3.ServiceUrl,
                });

        private readonly ILogger _logger = logger.CreateLogger<ReadRepository>();

        private static readonly ActivitySource _activitySource = new(nameof(MessagePublisher));

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

            var request = new GetObjectRequest()
            {
                BucketName = _options.S3.Buckets["Publisher"],
                Key = path
            };
            try
            {
                var response = await _amazonS3.GetObjectAsync(request);

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
