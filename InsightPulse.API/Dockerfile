# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY InsightPulse.slnx .
COPY InsightPulse.API/  InsightPulse.API/
COPY InsightPulse.Core/ InsightPulse.Core/

RUN dotnet restore InsightPulse.slnx
RUN dotnet publish InsightPulse.API/InsightPulse.API.csproj \
      -c Release -o /app/publish --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install curl for the Docker healthcheck.
# aspnet:9.0 is based on debian:bookworm-slim which does NOT include curl or wget.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 5228
ENV ASPNETCORE_URLS=http://+:5228
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "InsightPulse.API.dll"]
