using Amazon.SQS;

namespace Palantiri.Shared.SQS
{
    public class AmazonS3Options
    {

        public string Endpoint { get; set; } = "";

        public string ServiceUrl { get; set; } = "";

        public IDictionary<string, string> Buckets { get; set; } = new Dictionary<string, string>();
    }
}