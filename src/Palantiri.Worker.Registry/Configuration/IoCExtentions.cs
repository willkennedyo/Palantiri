using Palantiri.Shared.Amazon.SQS;
using Palantiri.Shared.Consumer;
using Palantiri.Shared.SQS;
using Palantiri.Shared.Worker;

namespace Palantiri.Worker.Registry.Configuration
{
    public static class IoCExtentions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {

            services.Configure<AmazonOptions>(
                config.GetSection(AmazonOptions.Amazon));

            services.AddSingleton<IMessageConsumer, MessageConsumer>();
            services.AddSingleton<IWorkerEventHandler<WorkerEvent>, WorkerEventHandler>();

            return services;
        }
    }
}
