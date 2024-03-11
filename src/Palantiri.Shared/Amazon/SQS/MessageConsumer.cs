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
using OpenTelemetry.Context.Propagation;
using Palantiri.Shared.Observability.TraceContext;

namespace Palantiri.Shared.Amazon.SQS
{

    public class MessageConsumer : IMessageConsumer
    {
        private readonly AmazonSQSClient _amazonSQS;
        private readonly AmazonOptions _options;

        private readonly ILogger _logger;

        private static readonly ActivitySource _activitySource = new(nameof(MessageConsumer));
        private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;
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

            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
            
            using (var activity = _activitySource.StartActivity("AWS:SQS:ConsumeMessages", ActivityKind.Consumer, null, links: null))

            {
                ReceiveMessageResponse receiveMessageResponse = await ReceiveMessages();

                ConcurrentBag<Activity> activities = [];

                for (int i = 0; i < receiveMessageResponse.Messages.Count; i++)
                {
                    var message = receiveMessageResponse.Messages[i];
                    // Extract the PropagationContext of the upstream parent from the message headers.
                    var parentContext = _propagator.Extract(default, message.MessageAttributes, AmazonTraceContext.ExtractTraceContextFromBasicProperties);
                    //Baggage.Current = parentContext.Baggage;
                    activity?.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);

                    using (var activityWithLinks = _activitySource.StartActivity("AWS:SQS:Consume", ActivityKind.Consumer, parentContext.ActivityContext, links: new List<ActivityLink>() { new(activity!.Context) }))
                    {
                        if (activityWithLinks is not null)
                            activities.Add(activityWithLinks);

                        activity.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);
                        activityWithLinks?.AddEvent(new ActivityEvent("Message received"));

                        activityWithLinks?.AddTag("message", message.Body);
                        activityWithLinks?.AddTag("message-id", message.MessageId);
                    }
                }
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


        public Task<T> ConsumeAsync<T>() where T : IIdentifiable<Guid>
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> ConsumeListAsync<T>() where T : IIdentifiable<Guid>
        {
            throw new NotImplementedException();
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
