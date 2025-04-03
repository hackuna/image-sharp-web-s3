using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp.Web.Resolvers;

namespace ImageStorageProject.ImageStorage;

internal sealed class ImageCacheResolver(IAmazonS3 amazonS3, string bucketName, string imagePath, MetadataCollection metadata) : IImageCacheResolver
{
    public Task<ImageCacheMetadata> GetMetaDataAsync()
    {
        Dictionary<string, string> dict = [];

        foreach (string key in metadata.Keys)
        {
            dict.Add(key.Substring(11).ToUpperInvariant(), metadata[key]);
        }

        return Task.FromResult(ImageCacheMetadata.FromDictionary(dict));
    }

    public Task<Stream> OpenReadAsync()
    {
        return amazonS3.GetObjectStreamAsync(bucketName, imagePath, null);
    }
}
