namespace JobRecon.Profile.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}
