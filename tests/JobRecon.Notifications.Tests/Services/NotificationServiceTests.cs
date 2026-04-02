using FluentAssertions;
using JobRecon.Notifications.Domain;
using JobRecon.Notifications.Infrastructure;
using JobRecon.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobRecon.Notifications.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly NotificationsDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new NotificationsDbContext(options);
        _cache = Substitute.For<IDistributedCache>();
        _logger = Substitute.For<ILogger<NotificationService>>();

        _sut = new NotificationService(_dbContext, _cache, _logger);
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldCreateNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Test Notification";
        var body = "This is a test notification";

        // Act
        var result = await _sut.CreateNotificationAsync(
            userId,
            NotificationType.NewMatch,
            NotificationChannel.InApp,
            title,
            body);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Title.Should().Be(title);
        result.Body.Should().Be(body);
        result.Type.Should().Be(NotificationType.NewMatch);
        result.Channel.Should().Be(NotificationChannel.InApp);
        result.IsRead.Should().BeFalse();

        var saved = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnUserNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await _sut.CreateNotificationAsync(userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 1", "Body 1");
        await _sut.CreateNotificationAsync(userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 2", "Body 2");
        await _sut.CreateNotificationAsync(otherUserId, NotificationType.NewMatch, NotificationChannel.InApp, "Other", "Body");

        // Act
        var result = await _sut.GetUserNotificationsAsync(userId);

        // Assert
        result.Notifications.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Notifications.Should().OnlyContain(n => n.Title.StartsWith("Test"));
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithUnreadOnlyFilter_ShouldReturnOnlyUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var notification1 = await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 1", "Body 1");
        await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 2", "Body 2");

        await _sut.MarkAsReadAsync(userId, notification1.Id);

        // Act
        var result = await _sut.GetUserNotificationsAsync(userId, unreadOnly: true);

        // Assert
        result.Notifications.Should().HaveCount(1);
        result.Notifications[0].Title.Should().Be("Test 2");
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test", "Body");

        // Act
        var result = await _sut.MarkAsReadAsync(userId, notification.Id);

        // Assert
        result.Should().BeTrue();

        var updated = await _dbContext.Notifications.FindAsync(notification.Id);
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithWrongUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        var notification = await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test", "Body");

        // Act
        var result = await _sut.MarkAsReadAsync(wrongUserId, notification.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Skip = "ExecuteUpdate not supported by InMemory provider")]
    public async Task MarkAllAsReadAsync_ShouldMarkAllNotificationsAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await _sut.CreateNotificationAsync(userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 1", "Body 1");
        await _sut.CreateNotificationAsync(userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 2", "Body 2");
        await _sut.CreateNotificationAsync(userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 3", "Body 3");

        // Act
        var result = await _sut.MarkAllAsReadAsync(userId);

        // Assert
        result.Should().Be(3);

        var unreadCount = await _sut.GetUnreadCountAsync(userId);
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var notification1 = await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 1", "Body 1");
        await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 2", "Body 2");
        await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp, "Test 3", "Body 3");

        await _sut.MarkAsReadAsync(userId, notification1.Id);

        // Act
        var result = await _sut.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task HasEventBeenProcessedAsync_ShouldReturnTrueForProcessedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await _sut.CreateNotificationAsync(
            userId, NotificationType.NewMatch, NotificationChannel.InApp,
            "Test", "Body", eventId: eventId);

        // Act
        var result = await _sut.HasEventBeenProcessedAsync(eventId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasEventBeenProcessedAsync_ShouldReturnFalseForUnprocessedEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _sut.HasEventBeenProcessedAsync(eventId);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
