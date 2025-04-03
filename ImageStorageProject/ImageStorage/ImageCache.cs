using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Resolvers;

namespace ImageStorageProject.ImageStorage;

internal sealed class ImageCache : IImageCache
{
    private readonly IAmazonS3 _amazonS3Client;
    private readonly string _bucketName;

    public ImageCache(IConfiguration configuration)
    {
        _amazonS3Client = new AmazonS3Client(
            configuration["MINIO_ROOT_USER"],
            configuration["MINIO_ROOT_PASSWORD"],
            new AmazonS3Config
            {
                AuthenticationRegion = configuration["MINIO_REGION_NAME"],
                ServiceURL = configuration["MINIO_ENDPOINT"],
                ForcePathStyle = true,
            });

        _bucketName = configuration["MINIO_CACHE_BUCKET"];
    }

    public async Task<IImageCacheResolver?> GetAsync(string key)
    {
        GetObjectMetadataRequest request = new() { BucketName = _bucketName, Key = key };

        try
        {
            MetadataCollection metadata = (await _amazonS3Client.GetObjectMetadataAsync(request)).Metadata;

            return new ImageCacheResolver(_amazonS3Client, _bucketName, key, metadata);
        }
        catch
        {
            return null;
        }
    }

    public Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
    {
        PutObjectRequest request = new()
        {
            BucketName = _bucketName,
            Key = key,
            ContentType = metadata.ContentType,
            InputStream = stream,
            AutoCloseStream = false
        };

        foreach (KeyValuePair<string, string> d in metadata.ToDictionary())
        {
            request.Metadata.Add(d.Key, d.Value);
        }

        return _amazonS3Client.PutObjectAsync(request);
    }
}
