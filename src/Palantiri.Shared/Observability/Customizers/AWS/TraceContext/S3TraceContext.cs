using Amazon.Runtime;
using Amazon.S3.Model;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace Palantiri.Shared.Observability.Customizers.AWS.TraceContext
{
    public static class S3TraceContext
    {
        private const int _maxMessageAttributes = 10;
        internal static void AddAttributes(IRequestContext context, IReadOnlyDictionary<string, string> attributes)
        {
            var originalRequest = context.OriginalRequest as PutObjectRequest;
            if (originalRequest?.Metadata == null)
            {
                return;
            }

            if (attributes.Keys.Any(k => originalRequest?.Metadata.Keys.Contains(k) == true ))
            {
                // If at least one attribute is already present in the request then we skip the injection.
                return;
            }

            int attributesCount = attributes.Keys.Count();
            if (attributes.Count + attributesCount > _maxMessageAttributes)
            {
                // TODO: add logging (event source).
                return;
            }

            foreach (var param in attributes)
            {
                originalRequest?.Metadata.Add(param.Key, param.Value);
            }
        }


        internal static void TraceAttributes(IResponseContext context, Activity activity, ActivitySource activitySource)
        {
            var response = context.Response as GetObjectResponse;
            if (response?.Metadata == null)
            {
                return;
            }

            var parentContext = ExtractIntoDictionary(response.Metadata);

            activity?.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);

            using var activityWithLinks = activitySource.StartActivity("AWS:S3:Readed", ActivityKind.Consumer, parentContext.ActivityContext, links: [new(activity!.Context)]);
            activity.SetParentId(parentContext.ActivityContext.TraceId, parentContext.ActivityContext.SpanId, ActivityTraceFlags.Recorded);
            activityWithLinks?.AddEvent(new ActivityEvent("Message received"));

            activityWithLinks?.AddTag("bucket-name", response.BucketName);
            //activityWithLinks?.AddTag("file-path", response);

            activityWithLinks?.SetStatus(response.HttpStatusCode == System.Net.HttpStatusCode.OK ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        }

        internal static IReadOnlyDictionary<string, string> InjectIntoDictionary(PropagationContext propagationContext)
        {
            var carrier = new Dictionary<string, string>();
            Propagators.DefaultTextMapPropagator.Inject(propagationContext, carrier, (c, k, v) => c[k] = v);
            return carrier;
        }

        internal static PropagationContext ExtractIntoDictionary(MetadataCollection metadata)
        {
            return Propagators.DefaultTextMapPropagator.Extract(default, metadata, ExtractTraceContextFromBasicProperties);
        }

        public static IEnumerable<string> ExtractTraceContextFromBasicProperties(MetadataCollection metadata, string key)
        {
            try
            {
                if (metadata[key] != null)
                {
                    return [metadata[key]];
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
