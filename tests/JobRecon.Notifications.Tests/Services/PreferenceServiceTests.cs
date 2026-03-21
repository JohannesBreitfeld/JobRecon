using FluentAssertions;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;
using JobRecon.Notifications.Infrastructure;
using JobRecon.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobRecon.Notifications.Tests.Services;

public class PreferenceServiceTests : IDisposable
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ILogger<PreferenceService> _logger;
    private readonly PreferenceService _sut;

    public PreferenceServiceTests()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new NotificationsDbContext(options);
        _logger = Substitute.For<ILogger<PreferenceService>>();

        _sut = new PreferenceService(_dbContext, _logger);
    }

    [Fact]
    public async Task GetOrCreatePreferencesAsync_ShouldCreateDefaultPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.GetOrCreatePreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.EmailEnabled.Should().BeTrue();
        result.InAppEnabled.Should().BeTrue();
        result.DigestEnabled.Should().BeTrue();
        result.DigestFrequency.Should().Be(DigestFrequency.Daily);
        result.MinMatchScoreForRealtime.Should().Be(0.8);

        var saved = await _dbContext.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreatePreferencesAsync_ShouldReturnExistingPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existing = NotificationPreference.CreateDefault(userId);
        existing.EmailEnabled = false;
        _dbContext.NotificationPreferences.Add(existing);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetOrCreatePreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existing.Id);
        result.EmailEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ShouldUpdatePreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _sut.GetOrCreatePreferencesAsync(userId);

        var updateRequest = new UpdatePreferencesRequest(
            EmailEnabled: false,
            InAppEnabled: true,
            DigestEnabled: false,
            DigestFrequency: DigestFrequency.Weekly,
            DigestTime: new TimeOnly(9, 30),
            MinMatchScoreForRealtime: 0.9,
            OverrideEmail: "custom@example.com");

        // Act
        var result = await _sut.UpdatePreferencesAsync(userId, updateRequest);

        // Assert
        result.EmailEnabled.Should().BeFalse();
        result.InAppEnabled.Should().BeTrue();
        result.DigestEnabled.Should().BeFalse();
        result.DigestFrequency.Should().Be(DigestFrequency.Weekly);
        result.DigestTime.Should().Be(new TimeOnly(9, 30));
        result.MinMatchScoreForRealtime.Should().Be(0.9);
        result.OverrideEmail.Should().Be("custom@example.com");
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var original = await _sut.GetOrCreatePreferencesAsync(userId);
        var originalDigestTime = original.DigestTime;

        var updateRequest = new UpdatePreferencesRequest(EmailEnabled: false);

        // Act
        var result = await _sut.UpdatePreferencesAsync(userId, updateRequest);

        // Assert
        result.EmailEnabled.Should().BeFalse();
        result.InAppEnabled.Should().BeTrue(); // Unchanged
        result.DigestEnabled.Should().BeTrue(); // Unchanged
        result.DigestTime.Should().Be(originalDigestTime); // Unchanged
    }

    [Fact]
    public async Task GetUsersReadyForDigestAsync_ShouldReturnUsersInTimeWindow()
    {
        // Arrange
        var currentTime = new TimeOnly(8, 30);

        var user1 = NotificationPreference.CreateDefault(Guid.NewGuid());
        user1.DigestTime = new TimeOnly(8, 0);
        user1.DigestFrequency = DigestFrequency.Daily;
        user1.DigestEnabled = true;

        var user2 = NotificationPreference.CreateDefault(Guid.NewGuid());
        user2.DigestTime = new TimeOnly(8, 45);
        user2.DigestFrequency = DigestFrequency.Daily;
        user2.DigestEnabled = true;

        var user3 = NotificationPreference.CreateDefault(Guid.NewGuid());
        user3.DigestTime = new TimeOnly(9, 0);
        user3.DigestFrequency = DigestFrequency.Daily;
        user3.DigestEnabled = true;

        var user4 = NotificationPreference.CreateDefault(Guid.NewGuid());
        user4.DigestTime = new TimeOnly(8, 15);
        user4.DigestFrequency = DigestFrequency.Weekly;
        user4.DigestEnabled = true;

        _dbContext.NotificationPreferences.AddRange(user1, user2, user3, user4);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetUsersReadyForDigestAsync(DigestFrequency.Daily, currentTime);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.UserId == user1.UserId);
        result.Should().Contain(p => p.UserId == user2.UserId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
