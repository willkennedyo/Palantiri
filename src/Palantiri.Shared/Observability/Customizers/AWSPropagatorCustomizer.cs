using Amazon.Runtime.Internal;
using Amazon.Runtime;
using OpenTelemetry.Contrib.Instrumentation.AWS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Observability.Customizers
{
    internal class AWSPropagatorCustomizer : IRuntimePipelineCustomizer
    {
        public string UniqueName => "AWS Tracing Propagator Registration Customization";

        private readonly AWSClientInstrumentationOptions options;

        public AWSPropagatorCustomizer(AWSClientInstrumentationOptions options)
        {
            this.options = options;
        }

        public void Customize(Type serviceClientType, RuntimePipeline pipeline)
        {
            if (!typeof(AmazonServiceClient).IsAssignableFrom(serviceClientType))
            {
                return;
            }

            var tracingPipelineHandler = new AWSPropagatorHandler(this.options);
            //var propagatingPipelineHandler = new AWSPropagatorPipelineHandler(tracingPipelineHandler);

            // AWSTracingPipelineHandler must execute early in the AWS SDK pipeline
            // in order to manipulate outgoing requests objects before they are marshalled (ie serialized).
            pipeline.AddHandlerBefore<Marshaller>(tracingPipelineHandler);

            // AWSPropagatorPipelineHandler executes after the AWS SDK has marshalled (ie serialized)
            // the outgoing request object so that it can work with the request's Headers
            //pipeline.AddHandlerBefore<RetryHandler>(propagatingPipelineHandler);
        }
    }
}
