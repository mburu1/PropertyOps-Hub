# syntax=docker/dockerfile:1

# ---- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Directory.Build.props pins TargetFramework=net10.0 and Directory.Packages.props
# carries the central package versions, so both must be in the context.
# global.json is deliberately NOT copied: it pins an exact SDK feature band for
# local development, and the image's 10.0 SDK must be free to satisfy it.
COPY Directory.Build.props Directory.Packages.props ./

# Copy project files first so `dotnet restore` is cached independently of source edits.
COPY src/PropertyOpsHub.Domain/PropertyOpsHub.Domain.csproj src/PropertyOpsHub.Domain/
COPY src/PropertyOpsHub.Application/PropertyOpsHub.Application.csproj src/PropertyOpsHub.Application/
COPY src/PropertyOpsHub.Infrastructure/PropertyOpsHub.Infrastructure.csproj src/PropertyOpsHub.Infrastructure/
COPY src/PropertyOpsHub.Api/PropertyOpsHub.Api.csproj src/PropertyOpsHub.Api/
RUN dotnet restore src/PropertyOpsHub.Api/PropertyOpsHub.Api.csproj

COPY src/ src/
RUN dotnet publish src/PropertyOpsHub.Api/PropertyOpsHub.Api.csproj \
        --configuration Release \
        --output /app/publish \
        -p:UseAppHost=false

# ---- runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish ./

# Non-root user shipped with the .NET base images.
USER $APP_UID

ENTRYPOINT ["dotnet", "PropertyOpsHub.Api.dll"]
