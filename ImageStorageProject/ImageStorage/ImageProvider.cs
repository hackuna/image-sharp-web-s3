using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http.Extensions;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace ImageStorageProject.ImageStorage;

internal sealed partial class ImageProvider : IImageProvider
{
    private readonly FormatUtilities _formatUtilities;
    private readonly IAmazonS3 _amazonS3Client;

    public ProcessingBehavior ProcessingBehavior { get; } = ProcessingBehavior.All;

    public Func<HttpContext, bool> Match { get; set; } = ctx => ctx.Request.Path.StartsWithSegments("/preview");

    [GeneratedRegex(@"\/preview\/(?<image_bucket>[^?]+)\/(?<image_name>[^?]+)")]
    private static partial Regex MyRegex();

    public ImageProvider(IConfiguration configuration, FormatUtilities formatUtilities)
    {
        _formatUtilities = formatUtilities;
        _amazonS3Client = new AmazonS3Client(
            configuration["MINIO_ROOT_USER"],
            configuration["MINIO_ROOT_PASSWORD"],
            new AmazonS3Config
            {
                AuthenticationRegion = configuration["MINIO_REGION_NAME"],
                ServiceURL = configuration["MINIO_ENDPOINT"],
                ForcePathStyle = true,
            });
    }

    public async Task<IImageResolver?> GetAsync(HttpContext context)
    {
        var match = MyRegex().Match(context.Request.Path);

        if (match.Success)
        {
            KeyExistsResult keyExists = await KeyExists(
                _amazonS3Client,
                match.Groups["image_bucket"].Value,
                match.Groups["image_name"].Value);

            if (!keyExists.Exists)
            {
                return null;
            }

            return new ImageResolver(
                _amazonS3Client,
                match.Groups["image_bucket"].Value,
                match.Groups["image_name"].Value,
                keyExists.metadata);
        }

        return null;
    }

    public bool IsValidRequest(HttpContext context) =>
        _formatUtilities.TryGetExtensionFromUri(
            context.Request.GetDisplayUrl(),
            out _);

    private static async Task<KeyExistsResult> KeyExists(IAmazonS3 s3Client, string bucketName, string key)
    {
        try
        {
            GetObjectMetadataRequest request = new()
            {
                BucketName = bucketName,
                Key = key,
            };

            GetObjectMetadataResponse metadata = await s3Client.GetObjectMetadataAsync(request);

            return new KeyExistsResult(metadata);
        }
        catch (AmazonS3Exception e)
        {
            if (string.Equals(e.ErrorCode, "NoSuchBucket", StringComparison.Ordinal))
            {
                return default;
            }

            if (string.Equals(e.ErrorCode, "NotFound", StringComparison.Ordinal))
            {
                return default;
            }

            if (string.Equals(e.ErrorCode, "Forbidden", StringComparison.Ordinal))
            {
                return default;
            }

            throw;
        }
    }

    private readonly record struct KeyExistsResult(GetObjectMetadataResponse metadata)
    {
        public bool Exists => metadata is not null;
    }
}
