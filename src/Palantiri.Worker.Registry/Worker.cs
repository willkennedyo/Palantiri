using Palantiri.Shared.Amazon.SQS;
using Palantiri.Shared.Consumer;
using Palantiri.Shared.Worker;

namespace Palantiri.Worker.Registry
{
    public class Worker(ILoggerFactory factory, IMessageConsumer consumer, IWorkerEventHandler<WorkerEvent> eventHandler) : BackgroundService
    {
        private readonly ILogger<Worker> _logger = factory.CreateLogger<Worker>();
        private readonly IMessageConsumer _consumer = consumer;
        private readonly IWorkerEventHandler<WorkerEvent> _eventHandler = eventHandler;

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    stoppingToken.ThrowIfCancellationRequested();

                    await _consumer.ConsumeAsync<WorkerEvent>(_eventHandler.Handle, stoppingToken);

                    await Task.CompletedTask.ConfigureAwait(false);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
