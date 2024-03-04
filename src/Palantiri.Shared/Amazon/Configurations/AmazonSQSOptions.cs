using Amazon.SQS;

namespace Palantiri.Shared.SQS
{
    public class AmazonSQSOptions
    {
        public const string AmazonSQS = "AmazonSQS";
        public string AccessKey { get; set; } = "";

        public string SecretKey { get; set; } = "";

        public string Endpoint { get; set; } = "";

        public string ServiceUrl { get; set; } = "";

        public IDictionary<string, string> Queues { get; set; } = new Dictionary<string, string>();

        public int ItemsToConsume { get; set; } = 1;
        public int TimeoutSeconds { get; set; } = 0;
    }
}