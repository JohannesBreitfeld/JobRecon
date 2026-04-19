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

# Install Playwright Chromium browsers in the build stage (has SDK)
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN pwsh /app/publish/playwright.ps1 install --with-deps chromium

# Runtime stage — noble (Ubuntu 24.04) for Playwright Chromium compatibility
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Copy Playwright browsers and install only the OS-level dependencies
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
COPY --from=build /ms-playwright /ms-playwright
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        libnss3 libatk1.0-0t64 libatk-bridge2.0-0t64 libcups2t64 libdrm2 \
        libxkbcommon0 libxcomposite1 libxdamage1 libxrandr2 libxfixes3 libgbm1 \
        libpango-1.0-0 libcairo2 libasound2t64 libxshmfence1 libx11-xcb1 \
        fonts-liberation && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user (after browser install which needs root)
RUN useradd --no-create-home --shell /bin/false appuser && \
    chown -R appuser:appuser /ms-playwright
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "JobRecon.Jobs.dll"]
