namespace Palantiri.Shared.SQS
{
    public class AmazonS3Options
    {
        public const string AmazonS3 = "AmazonS3";
        public string AccessKey { get; set; } = "";

        public string SecretKey { get; set; } = "";

        public string Endpoint { get; set; } = "";

        public string ServiceUrl { get; set; } = "";

        public IDictionary<string, string> Buckets { get; set; } = new Dictionary<string, string>();

        public int ItemsToConsume { get; set; } = 1;
        public int TimeoutSeconds { get; set; } = 0;
    }
}