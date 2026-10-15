# ─────────────────────────────────────────────────────────────────────
#  JobOrbit Backend — Multi-stage Dockerfile
#  Target: Render.com (Web Service)
#
#  Image size ~120 MB (chiseled .NET 9 runtime, no SDK in final stage).
# ─────────────────────────────────────────────────────────────────────

# ─── Stage 1: build ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Cache NuGet restore by copying only .csproj / .sln first
COPY JoborbitApi.sln ./
COPY JoborbitApi/JoborbitApi.csproj ./JoborbitApi/
COPY JobsApi.Application/JobsApi.Application.csproj ./JobsApi.Application/
COPY JobsApi.Infrastructure/JobsApi.Infrastructure.csproj ./JobsApi.Infrastructure/
COPY JobsApi.Domain/JobsApi.Domain.csproj ./JobsApi.Domain/

RUN dotnet restore JoborbitApi.sln

# Now copy the actual source + build
COPY . ./
RUN dotnet publish JoborbitApi/JoborbitApi.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ─── Stage 2: runtime ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS runtime
WORKDIR /app

# Non-root user for security (chiseled images ship with `app` user)
USER app

# Copy the published output from the build stage
COPY --from=build /app/publish ./

# Render injects the PORT env var — ASP.NET listens on it automatically
# when we set ASPNETCORE_URLS=http://+:${PORT}.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_PRINT_TELEMETRY=false \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Health check: probe the /health endpoint every 30s
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fs http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "JoborbitApi.dll"]
