using Amazon.Runtime;
using Amazon.SQS.Model;
using OpenTelemetry.Context.Propagation;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Palantiri.Shared.Observability.Customizers.AWS.TraceContext
{
    public static class SQSTraceContext
    {
        private const int _maxMessageAttributes = 10;
        internal static void AddAttributes(IRequestContext context, IReadOnlyDictionary<string, string> attributes)
        {
            var originalRequest = context.OriginalRequest as SendMessageRequest;

            if (originalRequest?.MessageAttributes == null)
            {
                return;
            }

            if (attributes.Keys.Any(k => originalRequest.MessageAttributes.ContainsKey(k)))
            {
                // If at least one attribute is already present in the request then we skip the injection.
                return;
            }

            int attributesCount = originalRequest.MessageAttributes.Count;
            if (attributes.Count + attributesCount > _maxMessageAttributes)
            {
                // TODO: add logging (event source).
                return;
            }

            foreach (var param in attributes)
            {
                originalRequest.MessageAttributes[param.Key] = new MessageAttributeValue { DataType = "String", StringValue = param.Value };
            }
        }

        internal static void TraceAttributes(IResponseContext context, Activity activity, ActivitySource activitySource)
        {
            var response = context.Response as ReceiveMessageResponse;
            if (response?.Messages == null)
            {
                return;
            }

            ConcurrentBag<Activity> activities = [];

            for (int i = 0; i < response.Messages.Count; i++)
            {
                var message = response.Messages[i];

                var parentContext = ExtractIntoDictionary(message);

                using var activityWithLinks = activitySource.StartActivity("SQS.Consume", ActivityKind.Consumer, parentContext.ActivityContext, links: [new(activity!.Context)]);
                if (activityWithLinks is not null)
                    activities.Add(activityWithLinks);
                activity.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);
                activityWithLinks?.AddEvent(new ActivityEvent("Message received"));
                activityWithLinks?.AddTag("message", message.Body);
                activityWithLinks?.AddTag("message-id", message.MessageId);
            }
        }

        internal static IReadOnlyDictionary<string, string> InjectIntoDictionary(PropagationContext propagationContext)
        {
            var carrier = new Dictionary<string, string>();
            Propagators.DefaultTextMapPropagator.Inject(propagationContext, carrier, (c, k, v) => c[k] = v);
            return carrier;
        }

        internal static PropagationContext ExtractIntoDictionary(Message message)
        {
            return Propagators.DefaultTextMapPropagator.Extract(default, message.MessageAttributes, ExtractTraceContextFromBasicProperties);
        }

        public static void InjectTraceContextIntoBasicProperties(Dictionary<string, MessageAttributeValue> props, string key, string value)
        {
            try
            {
                props ??= [];

                props[key] = new() { DataType = nameof(String), StringValue = value };
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static IEnumerable<string> ExtractTraceContextFromBasicProperties(Dictionary<string, MessageAttributeValue> props, string key)
        {
            try
            {
                if (props.TryGetValue(key, out var value))
                {
                    return [value.StringValue];
                }
            }
            catch (Exception)
            {
                throw;
            }

            return [];
        }

    }
}
