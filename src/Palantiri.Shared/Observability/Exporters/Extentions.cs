using Palantiri.Shared.Observability.Enums;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Palantiri.Shared.Observability.Configurations;

namespace Palantiri.Shared.Observability.Exporters
{
    internal static class Extentions
    {
        internal static MeterProviderBuilder ConfigureExporters(this MeterProviderBuilder meterProviderBuilder,
        OpenTelemetrySettings settings,
        Action<OtlpExporterOptions> exporterResource)
        {

            void ConfigureOtlpExporter(Action<OtlpExporterOptions> exporterResource) => meterProviderBuilder.AddOtlpExporter(exporterResource);

            void ConfigureConsoleExporter() => meterProviderBuilder.AddConsoleExporter();

            void ConfigureXRay() { };

            Configure(ConfigureOtlpExporter, ConfigureConsoleExporter, ConfigureXRay, settings, exporterResource);

            return meterProviderBuilder;
        }
        internal static OpenTelemetryLoggerOptions ConfigureExporters(this OpenTelemetryLoggerOptions loggerOptions,
        OpenTelemetrySettings settings,
        Action<OtlpExporterOptions> exporterResource)
        {

            void ConfigureOtlpExporter(Action<OtlpExporterOptions> exporterResource) => loggerOptions.AddOtlpExporter(exporterResource);

            void ConfigureConsoleExporter() => loggerOptions.AddConsoleExporter();

            void ConfigureXRay() { };

            Configure(ConfigureOtlpExporter, ConfigureConsoleExporter, ConfigureXRay, settings, exporterResource);

            return loggerOptions;
        }

        internal static TracerProviderBuilder ConfigureExporters(this TracerProviderBuilder tracerProviderBuilder,
        OpenTelemetrySettings settings,
        Action<OtlpExporterOptions> exporterResource)
        {
            void ConfigureOtlpExporter(Action<OtlpExporterOptions> exporterResource) => tracerProviderBuilder.AddOtlpExporter(exporterResource);

            void ConfigureConsoleExporter() => tracerProviderBuilder.AddConsoleExporter();

            void ConfigureXRay() => tracerProviderBuilder.AddXRayTraceId();

            Configure(ConfigureOtlpExporter, ConfigureConsoleExporter, ConfigureXRay, settings, exporterResource);

            return tracerProviderBuilder;
        }

        internal static void Configure(Action<Action<OtlpExporterOptions>> configureOtlp,
        Action configureConsole,
        Action configureXRay,
        OpenTelemetrySettings settings,
        Action<OtlpExporterOptions> exporterResource)
        {

            foreach (var exporter in settings.Exporters!)
            {
                switch (exporter)
                {
                    case EnumExporter.Otlp:
                        configureOtlp(exporterResource);
                        break;
                    case EnumExporter.Console:
                        configureConsole();
                        break;
                    case EnumExporter.XRay:
                        configureXRay();
                        break;
                }
            }
        }

    }
}
