using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Util;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Contrib.Instrumentation.AWS;
using OpenTelemetry.Trace;
using Palantiri.Shared.Observability.Customizers.AWS;
using Palantiri.Shared.Observability.Customizers.AWS.TraceContext;
using System.Diagnostics;
using System.Net;

namespace Palantiri.Shared.Observability.Customizers
{
    internal class AWSPropagatorHandler(AWSClientInstrumentationOptions options) : PipelineHandler
    {
        internal const string ACTIVITY_SOURCE_NAME = "Amazon.AWS.AWSClientInstrumentation";

        private static readonly ActivitySource _activitySource = new(ACTIVITY_SOURCE_NAME);

        private Activity? _activity { get; set; }

        public override void InvokeSync(IExecutionContext executionContext)
        {
            _activity = ProcessBeginRequest(executionContext);
            try
            {
                base.InvokeSync(executionContext);
            }
            catch (Exception ex)
            {
                if (_activity != null)
                {
                    ProcessException(_activity, ex);
                }

                throw;
            }
            finally
            {
                if (_activity != null)
                {
                    ProcessEndRequest(executionContext, _activity);
                }
            }
        }

        public override async Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            T? ret = null;

            _activity = ProcessBeginRequest(executionContext);
            try
            {
                ret = await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_activity != null)
                {
                    ProcessException(_activity, ex);
                }

                throw;
            }
            finally
            {
                if (_activity != null)
                {
                    ProcessEndRequest(executionContext, _activity);
                }
            }

            return ret;
        }
        private Activity? ProcessBeginRequest(IExecutionContext executionContext)
        {
            var requestContext = executionContext.RequestContext;
            var service = AWSServiceHelper.GetAWSServiceName(requestContext);
            var operation = AWSServiceHelper.GetAWSOperationName(requestContext);

            Activity? activity = _activitySource.StartActivity(service + "." + operation, ActivityKind.Producer);

            if (activity == null)
            {
                return null;
            }
            activity.Start();
            if (options.SuppressDownstreamInstrumentation)
            {
                SuppressInstrumentationScope.Enter();
            }

            if (activity.IsAllDataRequested)
            {
                activity.SetTag(Constants.AttributeAWSServiceName, service);
                activity.SetTag(Constants.AttributeAWSOperationName, operation);
                var client = executionContext.RequestContext.ClientConfig;
                if (client != null)
                {
                    var region = client.RegionEndpoint?.SystemName;
                    activity.SetTag(Constants.AttributeAWSRegion, region ?? AWSSDKUtils.DetermineRegion(client.ServiceURL));
                }

                AddRequestSpecificInformation(activity, requestContext, service);
            }

            return activity;
        }

        private static void ProcessEndRequest(IExecutionContext executionContext, Activity activity)
        {


            var responseContext = executionContext.ResponseContext;
            var requestContext = executionContext.RequestContext;

            var service = AWSServiceHelper.GetAWSServiceName(requestContext);

            if (activity.IsAllDataRequested)
            {
                if (Utils.GetTagValue(activity, Constants.AttributeAWSRequestId) == null)
                {
                    activity.SetTag(Constants.AttributeAWSRequestId, FetchRequestId(requestContext, responseContext));
                }

                var httpResponse = responseContext.HttpResponse;
                if (httpResponse != null)
                {
                    int statusCode = (int)httpResponse.StatusCode;

                    AddStatusCodeToActivity(activity, statusCode);
                    activity.SetTag(Constants.AttributeHttpResponseContentLength, httpResponse.ContentLength);
                    activity.SetStatus(httpResponse.StatusCode == HttpStatusCode.OK ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                }
            }

            activity.Stop(); 

            if (responseContext?.HttpResponse != null)
            {
                if (Constants.IsSqsService(service))
                {
                    SQSTraceContext.TraceAttributes(responseContext, activity, _activitySource);
                }
                else if (Constants.IsSnsService(service))
                {
                    //TO-DO
                }
                else if (Constants.IsS3Service(service))
                {
                    S3TraceContext.TraceAttributes(responseContext, activity, _activitySource);
                }
            }
        }

        private static void ProcessException(Activity activity, Exception ex)
        {
            if (activity.IsAllDataRequested)
            {
                activity.RecordException(ex);

                activity.SetStatus(Status.Error.WithDescription(ex.Message));

                if (ex is AmazonServiceException amazonServiceException)
                {
                    AddStatusCodeToActivity(activity, (int)amazonServiceException.StatusCode);
                    activity.SetTag(Constants.AttributeAWSRequestId, amazonServiceException.RequestId);
                }
            }
        }

        private static void AddRequestSpecificInformation(Activity activity, IRequestContext requestContext, string service)
        {
            if (AWSServiceHelper.ServiceParameterMap.TryGetValue(service, out var parameter))
            {
                AmazonWebServiceRequest request = requestContext.OriginalRequest;

                try
                {
                    var property = request.GetType().GetProperty(parameter);
                    if (property != null)
                    {
                        if (AWSServiceHelper.ParameterAttributeMap.TryGetValue(parameter, out var attribute))
                        {
                            activity.SetTag(attribute, property.GetValue(request));
                        }
                    }
                }
                catch (Exception)
                {
                    // Guard against any reflection-related exceptions when running in AoT.
                    // See https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/1543#issuecomment-1907667722.
                }
            }

            if (Constants.IsDynamoDbService(service))
            {
                activity.SetTag(Constants.AttributeDbSystem, Constants.AttributeValueDynamoDb);
            }
            else
            if (Constants.IsSqsService(service))
            {
                SQSTraceContext.AddAttributes(
                    requestContext, SQSTraceContext.InjectIntoDictionary(new PropagationContext(activity.Context, Baggage.Current)));
            }
            else if (Constants.IsSnsService(service))
            {
                SNSTraceContext.AddAttributes(
                    requestContext, SNSTraceContext.InjectIntoDictionary(new PropagationContext(activity.Context, Baggage.Current)));
            }
            else if (Constants.IsS3Service(service))
            {
                S3TraceContext.AddAttributes(
                    requestContext, S3TraceContext.InjectIntoDictionary(new PropagationContext(activity.Context, Baggage.Current)));
            }
        }

        private static void AddStatusCodeToActivity(Activity activity, int status_code)
        {
            activity.SetTag(Constants.AttributeHttpStatusCode, status_code);
        }

        private static string FetchRequestId(IRequestContext requestContext, IResponseContext responseContext)
        {
            string request_id = string.Empty;
            var response = responseContext.Response;
            if (response != null)
            {
                request_id = response.ResponseMetadata.RequestId;
            }
            else
            {
                var request_headers = requestContext.Request.Headers;
                if (string.IsNullOrEmpty(request_id) && request_headers.TryGetValue("x-amzn-RequestId", out var req_id))
                {
                    request_id = req_id;
                }

                if (string.IsNullOrEmpty(request_id) && request_headers.TryGetValue("x-amz-request-id", out req_id))
                {
                    request_id = req_id;
                }

                if (string.IsNullOrEmpty(request_id) && request_headers.TryGetValue("x-amz-id-2", out req_id))
                {
                    request_id = req_id;
                }
            }

            return request_id;
        }
    }
}
