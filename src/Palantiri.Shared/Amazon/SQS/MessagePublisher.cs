using System.Diagnostics;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Palantiri.Shared.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Palantiri.Shared.Observability.TraceContext;

namespace Palantiri.Shared.Amazon.SQS
{

    public class MessagePublisher : IMessagePublisher
    {
        private readonly AmazonSQSClient _amazonSQS;
        private readonly AmazonOptions _options;

        private readonly ILogger _logger;

        private static readonly ActivitySource _activitySource = new(nameof(MessagePublisher));
        private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;
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


            var request = new SendMessageRequest()
            {
                //MessageGroupId = message.GroupId,
                //MessageDeduplicationId = message.DeduplicationId,
                MessageBody = JsonSerializer.Serialize(message),
                QueueUrl = _options.SQS.Queues["Publisher"],
                MessageAttributes = props
            };
            var response = await _amazonSQS.SendMessageAsync( request);
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
