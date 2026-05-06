# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY InsightPulse.slnx .
COPY InsightPulse.API/ InsightPulse.API/
COPY InsightPulse.Core/ InsightPulse.Core/

RUN dotnet restore InsightPulse.slnx
RUN dotnet publish InsightPulse.API/InsightPulse.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5228
ENV ASPNETCORE_URLS=http://+:5228
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "InsightPulse.API.dll"]