# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Npgsql probes for GSSAPI/Kerberos (libgssapi_krb5.so.2) while negotiating auth; the slim
# runtime image doesn't ship it, which logs "Cannot load library libgssapi_krb5.so.2".
# We use username/password auth (so it's harmless), but install the lib to silence it.
# curl backs the container health check in docker-compose.prod.yml.
# Installed as root, before dropping to the non-root app user.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central Package Management: PackageReference items carry no Version attribute, so restore
# fails with NU1015 unless this file lands before it. Copied with the csproj files so the
# restore layer stays cached until a package version actually changes.
COPY ["Directory.Packages.props", "./"]
COPY ["src/DevStart.WebApi/DevStart.WebApi.csproj", "src/DevStart.WebApi/"]
COPY ["src/DevStart.Infrastructure/DevStart.Infrastructure.csproj", "src/DevStart.Infrastructure/"]
COPY ["src/DevStart.Application/DevStart.Application.csproj", "src/DevStart.Application/"]
COPY ["src/DevStart.Domain/DevStart.Domain.csproj", "src/DevStart.Domain/"]
COPY ["src/DevStart.SharedKernel/DevStart.SharedKernel.csproj", "src/DevStart.SharedKernel/"]
RUN dotnet restore "./src/DevStart.WebApi/DevStart.WebApi.csproj"
COPY . .
WORKDIR "/src/src/DevStart.WebApi"
# Publish directly. A separate `dotnet build` step writing to a different output directory would
# only be recompiled from scratch by publish, doubling image build time — and nothing consumed it.
RUN dotnet publish "./DevStart.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DevStart.WebApi.dll"]