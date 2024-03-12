using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Palantiri.Shared.Dtos;
using Palantiri.Shared.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Palantiri.Shared.Amazon.SQS
{

    public class MessageConsumer : IMessageConsumer
    {
        private readonly AmazonSQSClient _amazonSQS;
        private readonly AmazonOptions _options;

        private readonly ILogger _logger;

        private static readonly ActivitySource _activitySource = new(nameof(MessageConsumer));
        public MessageConsumer(IOptions<AmazonOptions> options, ILoggerFactory logger)
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

        public async Task ConsumeAsync<T>(Func<IEnumerable<T>,CancellationToken, Task<IEnumerable<T>>> handler, CancellationToken token) where T : IIdentifiable<Guid>
        {
            using var activity = _activitySource.StartActivity("AWS:SQS:ConsumeMessages", ActivityKind.Consumer, null, links: null);
            ReceiveMessageResponse receiveMessageResponse = await ReceiveMessages();

            ConcurrentBag<Activity> activities = [];

            Dictionary<Guid, Message> messagesById = [];
            List<T> messagesToProcess = [];

            for (int i = 0; i < receiveMessageResponse.Messages.Count; i++)
            {
                var message = receiveMessageResponse.Messages[i];
                var deserialazedMessage = JsonSerializer.Deserialize<T>(message.Body);
                if (deserialazedMessage == null) continue;

                messagesToProcess.Add(deserialazedMessage);
                messagesById.Add(deserialazedMessage.Id, message);
            }

            var handlerResult = await handler(messagesToProcess, token);

            var processedIds = handlerResult.Select(_ => _.Id);
            var messagesToDelete = messagesById.Where(_ => processedIds.Contains(_.Key)).Select(_ => _.Value);

            if (!messagesToDelete.Any())
                return;
            await DeleteMessagesAsync(messagesToDelete);

            activity?.Stop();
        }

        private async Task<ReceiveMessageResponse> ReceiveMessages()
        {
            var receiveMessageRequest = BuildRequestWithAttributes();

            ReceiveMessageResponse receiveMessageResponse = await _amazonSQS.ReceiveMessageAsync(receiveMessageRequest);
            return receiveMessageResponse;
        }

        private ReceiveMessageRequest BuildRequestWithAttributes()
        {
            List<string> attributesList = ["All"];

            return new()
            {
                QueueUrl = _options.SQS.Queues["Consumer"],
                MessageAttributeNames = attributesList,
                MaxNumberOfMessages = _options.SQS.ItemsToConsume,
                WaitTimeSeconds = _options.SQS.TimeoutSeconds,
            };
        }


        public async Task<T> ConsumeAsync<T>() where T : IIdentifiable<Guid>
        {
            ReceiveMessageResponse receiveMessageResponse = await ReceiveMessages();

            if (receiveMessageResponse?.Messages.Any() != true)
                return default;
            
            await DeleteMessagesAsync(receiveMessageResponse.Messages.Take(1));
            
                var message = receiveMessageResponse.Messages.First();
            return JsonSerializer.Deserialize<T>(message.Body);
        }

        public async Task<IEnumerable<T>> ConsumeListAsync<T>() where T : IIdentifiable<Guid>
        {
            ReceiveMessageResponse receiveMessageResponse = await ReceiveMessages();
            
            if (receiveMessageResponse?.Messages.Any() != true)
                return default;

            Dictionary<Guid, Message> messagesById = [];
            List<T> messagesToProcess = [];

            for (int i = 0; i < receiveMessageResponse.Messages.Count; i++)
            {
                var message = receiveMessageResponse.Messages[i];
                var deserialazedMessage = JsonSerializer.Deserialize<T>(message.Body);
                if (deserialazedMessage == null) continue;

                messagesToProcess.Add(deserialazedMessage);
                messagesById.Add(deserialazedMessage.Id, message);
            }
            await DeleteMessagesAsync(receiveMessageResponse.Messages);
            return messagesToProcess;
        }

        public async Task DeleteMessagesAsync(IEnumerable<Message> messagesToDelete)
        {
            DeleteMessageBatchRequest request = new()
            {
                Entries = messagesToDelete.Select(_ => new DeleteMessageBatchRequestEntry()
                {
                    Id = _.MessageId,
                    ReceiptHandle = _.ReceiptHandle
                }).ToList(),
                QueueUrl = _options.SQS.Queues["Consumer"]
            };

            await _amazonSQS.DeleteMessageBatchAsync(request);
        }

    }
}
