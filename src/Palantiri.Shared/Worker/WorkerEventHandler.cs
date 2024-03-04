using Microsoft.Extensions.Logging;
using Palantiri.Shared.Amazon.SQS;
using Palantiri.Shared.Worker;

namespace Palantiri.Shared.Consumer
{
    public class WorkerEventHandler(ILoggerFactory factory) : IWorkerEventHandler<WorkerEvent>
    {
        private readonly ILogger<MessageConsumer> _logger = factory.CreateLogger<MessageConsumer>();

        public async Task<IEnumerable<WorkerEvent>> Handle(IEnumerable<WorkerEvent> events, CancellationToken token)
        {
            _logger.Log(LogLevel.Information, "Fuldaaa {events}", events);

            List<WorkerEvent> workerEvents = [];
            for (int i = 0; i< events.Count() && !token.IsCancellationRequested; i++)
            {
                bool sucess = await Process(events.ElementAt(i));
                if (sucess) 
                {
                    workerEvents.Add(events.ElementAt(i));
                }
            }
            return await Task.FromResult(workerEvents);
        }

        private async Task<bool> Process(WorkerEvent workerEvent)
        {
            _logger.Log(LogLevel.Information, "Fuldaaa {workerEvent}", workerEvent);
            //throw new NotImplementedException();
            return await Task.FromResult(true);
        }
    }
}
