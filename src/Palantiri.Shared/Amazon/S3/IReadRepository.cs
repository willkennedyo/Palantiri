namespace Palantiri.Shared.Amazon.S3
{
    public interface IReadRepository
    {
        Task<(Stream, string, string)> ReadAsync(string path);
    }
}
