using Palantiri.Shared.Dtos;

namespace Palantiri.Shared.Worker
{
    public class WorkerEvent : IIdentifiable<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();

    }
}
