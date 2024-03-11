using Palantiri.Shared.Amazon.SQS;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Palantiri.Shared.Observability.Exporters;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;

namespace Palantiri.Shared.Observability.Configurations
{
    internal static class Extentions
    {

        internal static TracerProviderBuilder ConfigureTracer(this TracerProviderBuilder tracerProvider,
            OpenTelemetrySettings settings,
            Action<OtlpExporterOptions> exporterResource,
            Func<ResourceBuilder> resource)
        {

            tracerProvider
                .SetResourceBuilder(resource().AddTelemetrySdk());

            foreach (var instrumentation in settings.Instrumentations!)
            {
                switch (instrumentation)
                {
                    case Enums.EnumInstrumentation.AspNetCore:
                        tracerProvider.AddAspNetCoreInstrumentation();
                        break;
                    case Enums.EnumInstrumentation.SqlClient:
                        // tracerProvider.AddSqlClientInstrumentation(options =>
                        //     {
                        //         options.SetDbStatementForText = true;
                        //         options.RecordException = true;
                        //     });
                        break;
                    case Enums.EnumInstrumentation.MySqlData:
                        // tracerProvider.AddMySqlDataInstrumentation(options =>
                        //     {
                        //         options.RecordException = true;
                        //     });
                        break;
                    case Enums.EnumInstrumentation.AWS:
                        tracerProvider.AddAWSInstrumentation();
                        break;
                    case Enums.EnumInstrumentation.HttpClient:
                    case Enums.EnumInstrumentation.Runtime:
                    case Enums.EnumInstrumentation.Process:
                    case Enums.EnumInstrumentation.Redis:
                    case Enums.EnumInstrumentation.XRayTraceId:
                        //tracerProvider.AddXRayTraceId();
                        break;
                }
            }

            tracerProvider.AddSource(settings.ServiceName, nameof(MessageConsumer), nameof(MessagePublisher));

            tracerProvider.ConfigureExporters(settings, exporterResource);
            return tracerProvider;
        }
        internal static MeterProviderBuilder ConfigureMeter(this MeterProviderBuilder meterProviderBuilder,
            OpenTelemetrySettings settings,
            Action<OtlpExporterOptions> exporterResource,
            Func<ResourceBuilder> resource)
        {
            meterProviderBuilder
                .SetResourceBuilder(resource().AddTelemetrySdk());

            foreach (var instrumentation in settings.Instrumentations!)
            {
                switch (instrumentation)
                {
                    case Enums.EnumInstrumentation.Runtime:
                        meterProviderBuilder.AddRuntimeInstrumentation();
                        break;
                    case Enums.EnumInstrumentation.AspNetCore:
                        meterProviderBuilder.AddAspNetCoreInstrumentation();
                        break;
                    case Enums.EnumInstrumentation.HttpClient:
                        meterProviderBuilder.AddHttpClientInstrumentation();
                        break;
                    case Enums.EnumInstrumentation.Process:
                        meterProviderBuilder.AddProcessInstrumentation();
                        break;
                    //case Enums.EnumInstrimentation.SqlClient:
                    //case Enums.EnumInstrimentation.MySqlData:
                    //case Enums.EnumInstrimentation.Redis:
                    //case Enums.EnumInstrimentation.AWS:
                    //case Enums.EnumInstrimentation.XRayTraceId:
                    default:
                        break;
                }
            }
            meterProviderBuilder
                .AddMeter(
                    "System.Runtime",
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    settings.MeterName);

            meterProviderBuilder.ConfigureExporters(settings, exporterResource);

            return meterProviderBuilder;
        }
    }
}
