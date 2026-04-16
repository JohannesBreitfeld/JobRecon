using Microsoft.Playwright;

namespace JobRecon.Jobs.Services;

public interface IPlaywrightPageFactory : IAsyncDisposable
{
    Task<IPage> CreatePageAsync();
}

public sealed class PlaywrightPageFactory : IPlaywrightPageFactory
{
    private readonly ILogger<PlaywrightPageFactory> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;

    public PlaywrightPageFactory(ILogger<PlaywrightPageFactory> logger)
    {
        _logger = logger;
    }

    public async Task<IPage> CreatePageAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync();

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "JobRecon/1.0 (Job aggregation service)",
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            Locale = "sv-SE",
            JavaScriptEnabled = true,
            IgnoreHTTPSErrors = true
        });

        // Block unnecessary resources to speed up page loads
        await context.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,ico,woff,woff2,ttf,eot}", route => route.AbortAsync());

        return await context.NewPageAsync();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_browser is not null)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_browser is not null)
                return;

            _logger.LogInformation("Initializing Playwright and launching Chromium");

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--disable-gpu", "--no-sandbox", "--disable-dev-shm-usage"]
            });

            _logger.LogInformation("Playwright Chromium browser launched successfully");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        _initLock.Dispose();
    }
}
