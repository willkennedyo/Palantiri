using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Palantiri.Shared.Observability.Configurations;
using Microsoft.AspNetCore.Builder;
using System.Diagnostics;
using Palantiri.Shared.Observability.Exporters;

namespace Palantiri.Shared.Observability
{
    public static class Extentions
    {
        public static IServiceCollection AddOpentelemetry(this IServiceCollection services, IConfiguration? config)
        {
            var settings = config
                        .GetSection(nameof(OpenTelemetrySettings))
                        .Get<OpenTelemetrySettings>()!;

            if (string.IsNullOrWhiteSpace(settings.Endpoint))
            {
                return services;
            }

            ResourceBuilder configureResource()
            {
                //resourceBuilder.AddService(serviceName: builder.Environment.ApplicationName);
                var name = settings.ServiceName;//?? typeof(AppContext).Assembly.GetName().FullName;
                var version = typeof(AppContext).Assembly.GetName().Version?.ToString() ?? "unknown";
                var env = Environment.MachineName;

                return ResourceBuilder.CreateDefault().AddService(
                    serviceName: name,
                    serviceVersion: version,
                    serviceInstanceId: env);
            }

            Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[]
            {
                new OpenTelemetry.Extensions.Propagators.B3Propagator()
                ,
                new BaggagePropagator(),
                new AWSXRayPropagator()
            }));

            void exporterResource(OtlpExporterOptions exporterOptions)
            {
                exporterOptions.Endpoint = new(settings.Endpoint);
            }

            services
                .AddOpenTelemetry()
                .WithMetrics(meterProvider =>
                {
                    meterProvider.ConfigureMeter(settings, exporterResource, configureResource);
                })
                .WithTracing(tracerProvider =>
                {
                    tracerProvider.ConfigureTracer(settings, exporterResource, configureResource);
                });

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddEventSourceLogger();
                builder.AddOpenTelemetry(logging =>
                {
                    logging.ConfigureExporters(settings, exporterResource);
                });
            });

            services.AddSingleton(loggerFactory);


            return services;

        }
        
        /// <summary>
         /// Adds a request-id in the headers of all requests, and internally treats it as the trace-id, in order to be propagated
         /// </summary>
         /// <param name="app">The Itself IAplicationBuilder</param>
         /// <returns>The Itself <paramref name="app"/></returns>
        public static IApplicationBuilder AddRequestIdOnResponseMiddleware(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("request-id", Activity.Current?.TraceId.ToString() ?? string.Empty);
                await next();
            });

        }
    }
}