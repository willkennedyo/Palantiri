using Amazon.S3.Model;
using Amazon.SQS.Model;

namespace Palantiri.Shared.Observability.TraceContext
{
    public static class AmazonTraceContext
    {

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
