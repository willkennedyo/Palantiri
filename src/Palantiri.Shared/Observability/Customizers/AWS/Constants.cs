using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Observability.Customizers.AWS
{
    internal static class Constants
    {
        public const string AttributeAWSServiceName = "aws.service";
        public const string AttributeAWSOperationName = "aws.operation";
        public const string AttributeAWSRegion = "aws.region";
        public const string AttributeAWSRequestId = "aws.requestId";

        public const string AttributeAWSDynamoTableName = "aws.table_name";
        public const string AttributeAWSSQSQueueUrl = "aws.queue_url";

        public const string AttributeHttpStatusCode = "http.status_code";
        public const string AttributeHttpResponseContentLength = "http.response_content_length";

        public const string AttributeValueDynamoDb = "dynamodb";

        public static string AttributeDbSystem = "db.system";


        internal const string DynamoDbService = "DynamoDB";
        internal const string SQSService = "SQS";
        internal const string SNSService = "SNS";
        internal const string S3Service = "S3";

        internal static bool IsDynamoDbService(string service)
            => DynamoDbService.Equals(service, StringComparison.OrdinalIgnoreCase);

        internal static bool IsSqsService(string service)
            => SQSService.Equals(service, StringComparison.OrdinalIgnoreCase);

        internal static bool IsSnsService(string service)
            => SNSService.Equals(service, StringComparison.OrdinalIgnoreCase);
        internal static bool IsS3Service(string service)
            => S3Service.Equals(service, StringComparison.OrdinalIgnoreCase);

    }
}
