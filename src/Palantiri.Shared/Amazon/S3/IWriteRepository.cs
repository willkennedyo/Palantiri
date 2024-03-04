namespace Palantiri.Shared.Amazon.S3
{
    public interface IWriteRepository
    {
        Task WriteAsync(Stream stream, string type, string path);
        Task DeleteAsync(string filePath);
    }
}
