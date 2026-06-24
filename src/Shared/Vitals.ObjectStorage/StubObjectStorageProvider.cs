using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vitals.ObjectStorage;

public sealed class StubObjectStorageProvider : IObjectStorageProvider
{
    private readonly ObjectStorageOptions _options;
    private readonly ILogger<StubObjectStorageProvider> _logger;

    public StubObjectStorageProvider(IOptions<ObjectStorageOptions> options, ILogger<StubObjectStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<StoredObjectResult> UploadAsync(StoreObjectRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Object storage stub upload -> {Key}", request.ObjectKey);
        var url = $"{_options.ServiceUrl?.TrimEnd('/') ?? "https://storage-stub.vitals.local"}/{_options.BucketName ?? "vitals"}/{request.ObjectKey}";
        var cdn = string.IsNullOrWhiteSpace(_options.CdnBaseUrl) ? null : $"{_options.CdnBaseUrl.TrimEnd('/')}/{request.ObjectKey}";
        return Task.FromResult(new StoredObjectResult
        {
            ObjectKey = request.ObjectKey,
            Url = url,
            CdnUrl = cdn,
            SizeBytes = request.Content.CanSeek ? request.Content.Length : 0,
            ContentType = request.ContentType
        });
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Object storage stub delete -> {Key}", objectKey);
        return Task.CompletedTask;
    }

    public Task<Stream?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Object storage stub download -> {Key}", objectKey);
        return Task.FromResult<Stream?>(null);
    }
}
