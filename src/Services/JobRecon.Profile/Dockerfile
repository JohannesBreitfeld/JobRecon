# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY JobRecon.slnx .
COPY src/Shared/JobRecon.Contracts/JobRecon.Contracts.csproj src/Shared/JobRecon.Contracts/
COPY src/Shared/JobRecon.Domain/JobRecon.Domain.csproj src/Shared/JobRecon.Domain/
COPY src/Shared/JobRecon.Infrastructure/JobRecon.Infrastructure.csproj src/Shared/JobRecon.Infrastructure/
COPY src/Shared/JobRecon.Protos/JobRecon.Protos.csproj src/Shared/JobRecon.Protos/
COPY src/Services/JobRecon.Profile/JobRecon.Profile.csproj src/Services/JobRecon.Profile/

# Restore dependencies
RUN dotnet restore src/Services/JobRecon.Profile/JobRecon.Profile.csproj

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/Services/JobRecon.Profile
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN useradd --no-create-home --shell /bin/false appuser
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "JobRecon.Profile.dll"]
