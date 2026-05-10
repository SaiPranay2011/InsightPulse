# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY InsightPulse.slnx .
COPY InsightPulse.API/ InsightPulse.API/
COPY InsightPulse.Core/ InsightPulse.Core/

RUN dotnet restore InsightPulse.slnx
RUN dotnet publish InsightPulse.API/InsightPulse.API.csproj -c Release -o /app/publish

# Runtime stage - Keep SDK for EF Core migrations
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# Install EF Core tools globally
RUN dotnet tool install --global dotnet-ef

# Add dotnet tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy published output
COPY --from=build /app/publish ./publish

# Copy source files needed by dotnet-ef for migrations
COPY InsightPulse.slnx .
COPY InsightPulse.API/ InsightPulse.API/
COPY InsightPulse.Core/ InsightPulse.Core/

# Restore so dotnet-ef can resolve the project
RUN dotnet restore InsightPulse.slnx

WORKDIR /app/publish

EXPOSE 5228
ENV ASPNETCORE_URLS=http://+:5228
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "InsightPulse.API.dll"]