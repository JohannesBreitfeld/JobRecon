namespace JobRecon.Notifications.Contracts;

public interface IProfileClient
{
    Task<UserEmailDto?> GetUserEmailAsync(Guid userId, CancellationToken ct = default);
}

public record UserEmailDto(string Email, string? DisplayName);

public class ProfileClient : IProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfileClient> _logger;

    public ProfileClient(HttpClient httpClient, ILogger<ProfileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserEmailDto?> GetUserEmailAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/profile/{userId}/email", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get email for user {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserEmailDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email for user {UserId}", userId);
            return null;
        }
    }
}
