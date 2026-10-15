# ─────────────────────────────────────────────────────────────────────
#  JobOrbit Backend — Multi-stage Dockerfile
#  Target: Render.com (Web Service)
#
#  Uses the standard aspnet:9.0 runtime (NOT chiseled) because:
#   - chiseled images lack ICU → globalization crash on startup
#   - chiseled images lack curl → HEALTHCHECK can't run
#   - chiseled images lack the 'app' user → can't USER app
#  Image size ~190 MB (vs ~120 MB chiseled). Trade-off: works out of the box.
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN groupadd --gid 1001 app \
 && useradd  --uid 1001 --gid app --shell /bin/bash --create-home app

# Copy the published output from the build stage
COPY --from=build /app/publish ./
RUN chown -R app:app /app

USER app

# Render injects the PORT env var — ASP.NET listens on it automatically
# when we set ASPNETCORE_URLS=http://+:${PORT}.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_PRINT_TELEMETRY=false

EXPOSE 8080

# Health check: probe the /health endpoint every 30s
# (curl is available in the standard aspnet:9.0 image)
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fs http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "JoborbitApi.dll"]
