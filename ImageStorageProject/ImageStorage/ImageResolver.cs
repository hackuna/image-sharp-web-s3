using System.Net.Http.Headers;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp.Web.Resolvers;

namespace ImageStorageProject.ImageStorage;

internal sealed class ImageResolver : IImageResolver
{
    private readonly IAmazonS3 _amazonS3;
    private readonly string _bucketName;
    private readonly string _imagePath;
    private readonly GetObjectMetadataResponse? _metadataResponse;

    public ImageResolver(
        IAmazonS3 amazonS3,
        string bucketName,
        string imagePath,
        GetObjectMetadataResponse? metadataResponse = null)
    {
        _amazonS3 = amazonS3;
        _bucketName = bucketName;
        _imagePath = imagePath;
        _metadataResponse = metadataResponse;
    }

    public async Task<ImageMetadata> GetMetaDataAsync()
    {
        var metadata = _metadataResponse
            ?? await _amazonS3.GetObjectMetadataAsync(_bucketName, _imagePath);

        var maxAge = TimeSpan.MinValue;

        if (CacheControlHeaderValue.TryParse(metadata.Headers.CacheControl, out CacheControlHeaderValue? cacheControl))
        {
            if (cacheControl?.MaxAge.HasValue == true)
            {
                maxAge = cacheControl.MaxAge.Value;
            }
        }

        return new ImageMetadata(metadata.LastModified, maxAge, metadata.ContentLength);
    }

    public Task<Stream> OpenReadAsync()
    {
        return _amazonS3.GetObjectStreamAsync(_bucketName, _imagePath, null);
    }
}
