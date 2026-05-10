# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY InsightPulse.slnx .
COPY InsightPulse.API/  InsightPulse.API/
COPY InsightPulse.Core/ InsightPulse.Core/

RUN dotnet restore InsightPulse.slnx

RUN dotnet publish InsightPulse.API/InsightPulse.API.csproj \
      -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
# Use the slim ASP.NET runtime image — no SDK, no source, no dotnet-ef needed.
# Database migrations run automatically via db.MigrateAsync() inside Program.cs
# at every startup, so there is nothing to exec from outside the container.
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5228
ENV ASPNETCORE_URLS=http://+:5228
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "InsightPulse.API.dll"]
