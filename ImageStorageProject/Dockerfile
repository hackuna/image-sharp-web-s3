# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

ENV MINIO_USER=""
ENV MINIO_PASSWORD=""
ENV MINIO_REGION=""
ENV MINIO_ENDPOINT=""
ENV MINIO_BUCKET=""
ENV MINIO_CACHE_BUCKET=""

LABEL org.opencontainers.image.base.name="mcr.microsoft.com/dotnet/aspnet:9.0"
LABEL org.opencontainers.image.url="ghcr.io/hackuna/image-storage-solution:latest"
LABEL org.opencontainers.image.title="Image Storage Solution"
LABEL org.opencontainers.image.description="S3 Image Storage"
LABEL org.opencontainers.image.version="1.0.0"
LABEL org.opencontainers.image.authors="hackuna"
LABEL org.opencontainers.image.vendor="https://github.com/hackuna"
LABEL org.opencontainers.image.source="https://github.com/hackuna/image-storage-solution"
LABEL org.opencontainers.image.documentation="https://github.com/hackuna/image-storage-solution/wiki"
LABEL org.opencontainers.image.licenses="MIT"

USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ImageStorageProject/ImageStorageProject.csproj", "ImageStorageProject/"]
RUN dotnet restore "./ImageStorageProject/ImageStorageProject.csproj"
COPY . .
WORKDIR "/src/ImageStorageProject"
RUN dotnet build "./ImageStorageProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ImageStorageProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageStorageProject.dll"]