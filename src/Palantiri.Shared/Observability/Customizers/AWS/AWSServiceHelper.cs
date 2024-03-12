using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Observability.Customizers.AWS
{

    internal class AWSServiceHelper
    {
        internal static IReadOnlyDictionary<string, string> ServiceParameterMap = new Dictionary<string, string>
    {
        { "DynamoDBv2", "TableName" },
        { "SQS", "QueueUrl" }
    };

        internal static IReadOnlyDictionary<string, string> ParameterAttributeMap = new Dictionary<string, string>
    {
        { "TableName", "aws.table_name" },
        { "QueueUrl", "aws.queue_url" }
    };

        private const string DynamoDbService = "DynamoDBv2";

        private const string SQSService = "SQS";

        internal static string GetAWSServiceName(IRequestContext requestContext)
        {
            return Utils.RemoveAmazonPrefixFromServiceName(requestContext.ClientConfig.ServiceId);
        }

        internal static string GetAWSOperationName(IRequestContext requestContext)
        {
            string name = requestContext.OriginalRequest.GetType().Name;
            string suffix = "Request";
            return Utils.RemoveSuffix(name, suffix);
        }
        internal static bool IsDynamoDbService(string service)
        {
            return "DynamoDBv2".Equals(service, StringComparison.OrdinalIgnoreCase);
        }
    }
}
