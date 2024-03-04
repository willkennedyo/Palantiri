using Palantiri.Rest.Configuration;
using Palantiri.Shared.Amazon;
using Palantiri.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddServices(builder.Configuration)
    .AddRepositories(builder.Configuration)
    .AddSQS(builder.Configuration)
    .AddS3(builder.Configuration)
    .AddOpentelemetry(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.AddRequestIdOnResponseMiddleware();

app.Run();
