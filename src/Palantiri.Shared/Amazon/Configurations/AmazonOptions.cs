namespace Palantiri.Shared.SQS
{
    public class AmazonOptions
    {
        public const string Amazon = "Amazon";
        public string AccessKey { get; set; } = "";

        public string SecretKey { get; set; } = "";

        public string Endpoint { get; set; } = "";
        public AmazonS3Options S3 { get; set; } = new();
        public AmazonSQSOptions SQS { get; set; } = new();
    }
}