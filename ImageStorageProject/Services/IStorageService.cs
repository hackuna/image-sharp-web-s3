using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;

namespace ImageStorageProject.Services;

internal interface IStorageService
{
    /// <summary>
    /// Upload files to S3
    /// </summary>
    /// <param name="files">Files collection</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>File keys array</returns>
    Task<IEnumerable<string>> UploadFilesAsync(IFormFileCollection files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create bucket
    /// </summary>
    /// <param name="bucket">Bucket name</param>
    /// <param name="isPublic">Is policy public?</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    Task CreateBucketAsync(string bucket, bool isPublic, CancellationToken cancellationToken = default);
}

internal sealed class StorageService(IConfiguration configuration) : IStorageService
{
    private AmazonS3Client s3Client = new(
            configuration["MINIO_ROOT_USER"],
            configuration["MINIO_ROOT_PASSWORD"],
            new AmazonS3Config
            {
                AuthenticationRegion = configuration["MINIO_REGION_NAME"],
                ServiceURL = configuration["MINIO_ENDPOINT"],
                ForcePathStyle = true,
            });

    public async Task<IEnumerable<string>> UploadFilesAsync(IFormFileCollection files, CancellationToken cancellationToken = default)
    {
        await CreateBucketAsync(configuration["MINIO_BUCKET"], false, cancellationToken);
        await CreateBucketAsync(configuration["MINIO_CACHE_BUCKET"], false, cancellationToken);

        var keys = new List<string>();

        try
        {
            foreach (var file in files)
            {
                await using var inputStream = file.OpenReadStream();

                using var fileTransferUtility = new TransferUtility(s3Client);

                var fileKey = $"{Guid.CreateVersion7():N}{Path.GetExtension(file.FileName)}";

                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = configuration["MINIO_BUCKET"],
                    Key = fileKey,
                    InputStream = inputStream,
                    ContentType = file.ContentType,
                    Metadata = {
                    ["Content-Type"] = file.ContentType,
                    ["Content-Length"] = file.Length.ToString(),
                    ["x-amz-meta-original-file-extension"] = Path.GetExtension(file.FileName),
                  }
                };

                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest, cancellationToken);

                keys.Add(fileKey);
            }

            return keys;
        }
        catch (AmazonS3Exception e)
        {
            throw new ApplicationException(e.Message, e);
        }
        catch (Exception e)
        {
            throw new ApplicationException(e.GetBaseException().Message, e.GetBaseException());
        }
    }

    public async Task CreateBucketAsync(string bucket, bool isPublic, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket, nameof(bucket));

        try
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucket))
            {
                if (!AmazonS3Util.ValidateV2Bucket(bucket))
                    throw new ApplicationException("invalid bucket name");

                await s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucket,
                    CannedACL = isPublic
                        ? S3CannedACL.PublicRead
                        : S3CannedACL.Private,
                }, cancellationToken);
            }
        }
        catch (AmazonS3Exception e)
        {
            throw new ApplicationException(e.Message, e);
        }
        catch (Exception e)
        {
            throw new ApplicationException(e.GetBaseException().Message, e.GetBaseException());
        }
    }
}
