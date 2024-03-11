using Palantiri.Shared.Observability.Enums;

namespace Palantiri.Shared.Observability.Configurations
{
    public class OpenTelemetrySettings
    {
        public string? Endpoint { get; set; }
        public string ServiceName { get; set; } = "Unknown";
        public EnumExporter[]? Exporters { get; set; }
        public EnumInstrumentation[]? Instrumentations { get; set; }
        public string MeterName { get; set; } = "defaultMeter";
    }
}
