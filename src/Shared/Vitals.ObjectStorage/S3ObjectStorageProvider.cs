using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vitals.ObjectStorage;

public sealed class S3ObjectStorageProvider : IObjectStorageProvider
{
    private readonly ObjectStorageOptions _options;
    private readonly ILogger<S3ObjectStorageProvider> _logger;
    private readonly IAmazonS3 _s3;

    public S3ObjectStorageProvider(IOptions<ObjectStorageOptions> options, ILogger<S3ObjectStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ServiceUrl) || string.IsNullOrWhiteSpace(_options.BucketName))
            throw new InvalidOperationException("ObjectStorage:ServiceUrl and BucketName are required when UseStub=false.");

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = _options.Region
        };

        _s3 = string.IsNullOrWhiteSpace(_options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task<StoredObjectResult> UploadAsync(StoreObjectRequest request, CancellationToken cancellationToken = default)
    {
        var put = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = request.ObjectKey,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(put, cancellationToken).ConfigureAwait(false);
        var url = $"{_options.ServiceUrl!.TrimEnd('/')}/{_options.BucketName}/{request.ObjectKey}";
        var cdn = string.IsNullOrWhiteSpace(_options.CdnBaseUrl) ? null : $"{_options.CdnBaseUrl.TrimEnd('/')}/{request.ObjectKey}";
        var sizeBytes = request.Content.CanSeek ? request.Content.Length : 0;

        return new StoredObjectResult
        {
            ObjectKey = request.ObjectKey,
            Url = url,
            CdnUrl = cdn,
            SizeBytes = sizeBytes,
            ContentType = request.ContentType
        };
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Deleted object {Key} from bucket {Bucket}", objectKey, _options.BucketName);
    }

    public async Task<Stream?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_options.BucketName, objectKey, cancellationToken).ConfigureAwait(false);
            var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
            memory.Position = 0;
            return memory;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
