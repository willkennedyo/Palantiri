using Palantiri.Shared.Dtos;

namespace Palantiri.Rest.Transport
{
    public class SQSMessage : Dictionary<string, string> , IIdentifiable<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();


    }
}
