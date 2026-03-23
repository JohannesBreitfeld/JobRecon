using JobRecon.Profile.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace JobRecon.Profile.Services;

public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(
        IMinioClient minioClient,
        IOptions<MinioSettings> settings,
        ILogger<MinioFileStorageService> logger)
    {
        _minioClient = minioClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var objectName = $"{Guid.NewGuid()}/{fileName}";

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        _logger.LogInformation("Uploaded file {FileName} to {ObjectName}", fileName, objectName);

        return objectName;
    }

    public async Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        try
        {
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(storagePath)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            return memoryStream;
        }
        catch
        {
            await memoryStream.DisposeAsync();
            throw;
        }
    }

    public async Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(storagePath);

        await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

        _logger.LogInformation("Deleted file at {StoragePath}", storagePath);
    }

    public async Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(storagePath);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(_settings.BucketName);

        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(_settings.BucketName);

            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

            _logger.LogInformation("Created bucket {BucketName}", _settings.BucketName);
        }
    }
}
