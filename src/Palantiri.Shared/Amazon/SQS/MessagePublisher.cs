using System.Diagnostics;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Palantiri.Shared.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Palantiri.Shared.Amazon.SQS
{

    public class MessagePublisher : IMessagePublisher
    {
        private readonly AmazonSQSClient _amazonSQS;
        private readonly AmazonOptions _options;

        private readonly ILogger _logger;

        private static readonly ActivitySource _activitySource = new(nameof(MessagePublisher));
        public MessagePublisher(IOptions<AmazonOptions> options, ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<MessagePublisher>();
            _options = options.Value;
            _amazonSQS = new AmazonSQSClient(
                new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
                new AmazonSQSConfig
                {
                    ServiceURL = _options.SQS.ServiceUrl
                });
        }

        public async Task PublishAsync<T>(T message) where T : class
        {

            using var activity = _activitySource.StartActivity("AWS:SQS:Publish", ActivityKind.Producer);

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }
            
            var request = new SendMessageRequest()
            {
                //MessageGroupId = message.GroupId,
                //MessageDeduplicationId = message.DeduplicationId,
                MessageBody = JsonSerializer.Serialize(message),
                QueueUrl = _options.SQS.Queues["Publisher"]
            };
            var response = await _amazonSQS.SendMessageAsync( request);

            activity?.Stop();
        }

        public async Task PublishAsync<T>(IEnumerable<T> messages) where T : class
        {
            var entries = new List<SendMessageBatchRequestEntry>();

            for (int i = 0; entries.Count < messages.Count(); i++)
            {
                var message = messages.ElementAt(i);

                string objJson = JsonSerializer.Serialize(message);

                var entry = new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), objJson);

                entries.Add(entry);
                i++;
            }

            await _amazonSQS.SendMessageBatchAsync(_options.SQS.Queues["Publisher"], entries);
        }
    }
}
