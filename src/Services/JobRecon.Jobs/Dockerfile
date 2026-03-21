# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY JobRecon.slnx .
COPY src/Shared/JobRecon.Contracts/JobRecon.Contracts.csproj src/Shared/JobRecon.Contracts/
COPY src/Shared/JobRecon.Domain/JobRecon.Domain.csproj src/Shared/JobRecon.Domain/
COPY src/Shared/JobRecon.Infrastructure/JobRecon.Infrastructure.csproj src/Shared/JobRecon.Infrastructure/
COPY src/Shared/JobRecon.Protos/JobRecon.Protos.csproj src/Shared/JobRecon.Protos/
COPY src/Services/JobRecon.Jobs/JobRecon.Jobs.csproj src/Services/JobRecon.Jobs/

# Restore dependencies
RUN dotnet restore src/Services/JobRecon.Jobs/JobRecon.Jobs.csproj

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/Services/JobRecon.Jobs
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "JobRecon.Jobs.dll"]
