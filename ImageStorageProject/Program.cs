using ImageStorageProject.ImageStorage;
using ImageStorageProject.Services;
using Scalar.AspNetCore;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddScoped<IStorageService, StorageService>();

string[] allowedImageExtensions = [".bmp", ".gif", ".jpeg", ".jpg", ".pbm", ".png", ".tif", ".tiff", ".tga", ".webp"];
string previewImageFormat = "webp";
string previewImageSize = "320";

builder.Services.AddImageSharp()
    .ClearProviders()
    .AddProvider<ImageProvider>()
    .SetCache<ImageCache>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.Servers = [new ScalarServer("http://localhost:5189")]);
}

app.UseImageSharp();

// Upload endpoint
app.MapPost("/upload", async (IStorageService storage, IFormFileCollection files, CancellationToken cancellationToken) =>
{
    if (!files.Any())
    {
        return Results.BadRequest("There is no files");
    }

    foreach (var file in files)
    {
        if (!allowedImageExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            return Results.BadRequest($"Unexpectable image format «{Path.GetExtension(file.FileName)}»");
        }
    }

    var fileKeys = await storage.UploadFilesAsync(files, cancellationToken);

    return Results.Ok(fileKeys.Select(s =>
        $"/preview/{builder.Configuration["MINIO_BUCKET"]}/{s}?format={previewImageFormat}&width={previewImageSize}"));

}).WithOpenApi().DisableAntiforgery();

await app.RunAsync();
