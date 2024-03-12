using Palantiri.Shared.Observability;
using Palantiri.Worker.Registry;
using Palantiri.Worker.Registry.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services
.AddOpentelemetry(builder.Configuration)
.AddServices(builder.Configuration);

var host = builder.Build();
host.Run();
