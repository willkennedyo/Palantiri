using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Palantiri.Shared.Amazon.S3;
using Palantiri.Shared.Amazon.SQS;
using Palantiri.Shared.SQS;

namespace Palantiri.Shared.Amazon
{
    public static class Extentions
    {
        public static IServiceCollection AddSQS(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<AmazonOptions>(
                config.GetSection(AmazonOptions.Amazon));

            services.AddScoped<IMessagePublisher, MessagePublisher>();
            services.AddScoped<IMessageConsumer, MessageConsumer>();

            return services;
        }
        public static IServiceCollection AddS3(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<AmazonOptions>(
                config.GetSection(AmazonOptions.Amazon));

            services.AddScoped<IReadRepository, ReadRepository>();
            services.AddScoped<IWriteRepository, WriteRepository>();

            return services;
        }
    }
}
