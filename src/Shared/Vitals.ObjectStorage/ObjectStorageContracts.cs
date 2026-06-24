namespace Vitals.ObjectStorage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public bool UseStub { get; set; } = true;
    public string Provider { get; set; } = "S3";
    public string? ServiceUrl { get; set; }
    public string? BucketName { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? CdnBaseUrl { get; set; }
    public string Region { get; set; } = "ru-central1";
}

public sealed class StoredObjectResult
{
    public required string ObjectKey { get; init; }
    public required string Url { get; init; }
    public string? CdnUrl { get; init; }
    public long SizeBytes { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
}

public sealed class StoreObjectRequest
{
    public required string ObjectKey { get; init; }
    public required Stream Content { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
}

public interface IObjectStorageProvider
{
    Task<StoredObjectResult> UploadAsync(StoreObjectRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);
}
