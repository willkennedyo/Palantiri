namespace Palantiri.Shared.Dtos
{
    public interface IIdentifiable<Guid>
    {
        public Guid Id { get; set; }
    }
}
