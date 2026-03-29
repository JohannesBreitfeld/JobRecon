using FluentAssertions;
using JobRecon.Profile.Contracts;
using JobRecon.Profile.Domain;
using JobRecon.Profile.Infrastructure;
using JobRecon.Profile.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobRecon.Profile.Tests.Services;

public class ProfileServiceTests : IDisposable
{
    private readonly ProfileDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ProfileService> _logger;
    private readonly ProfileService _sut;

    public ProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProfileDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ProfileDbContext(options);
        _fileStorage = Substitute.For<IFileStorageService>();
        _logger = Substitute.For<ILogger<ProfileService>>();
        _sut = new ProfileService(_dbContext, _fileStorage, _logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WhenProfileExists_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.CurrentJobTitle.Should().Be(profile.CurrentJobTitle);
    }

    [Fact]
    public async Task GetProfileAsync_WhenProfileNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Profile.NotFound");
    }

    #endregion

    #region CreateProfileAsync Tests

    [Fact]
    public async Task CreateProfileAsync_WithValidRequest_CreatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateProfileRequest
        {
            CurrentJobTitle = "Software Developer",
            Summary = "Test summary",
            Location = "Stockholm",
            DesiredJobTitles = ["Senior Developer", "Tech Lead"]
        };

        // Act
        var result = await _sut.CreateProfileAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.CurrentJobTitle.Should().Be(request.CurrentJobTitle);
        result.Value.DesiredJobTitles.Should().HaveCount(2);
        result.Value.DesiredJobTitles.Should().Contain("Senior Developer");
    }

    [Fact]
    public async Task CreateProfileAsync_WhenProfileExists_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProfile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(existingProfile);
        await _dbContext.SaveChangesAsync();

        var request = new CreateProfileRequest { CurrentJobTitle = "New Title" };

        // Act
        var result = await _sut.CreateProfileAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Profile.AlreadyExists");
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithValidRequest_UpdatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            CurrentJobTitle = "Updated Title",
            Summary = "Updated summary"
        };

        // Act
        var result = await _sut.UpdateProfileAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentJobTitle.Should().Be("Updated Title");
        result.Value.Summary.Should().Be("Updated summary");
    }

    [Fact(Skip = "InMemory provider doesn't properly handle concurrent entity updates. Use integration tests with real database.")]
    public async Task UpdateProfileAsync_WithNewJobTitles_ReplacesExistingTitles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var oldTitle = new DesiredJobTitle
        {
            Id = Guid.NewGuid(),
            Title = "Old Title",
            UserProfileId = profile.Id
        };
        _dbContext.DesiredJobTitles.Add(oldTitle);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            DesiredJobTitles = ["New Title 1", "New Title 2"]
        };

        // Act
        var result = await _sut.UpdateProfileAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DesiredJobTitles.Should().HaveCount(2);
        result.Value.DesiredJobTitles.Should().Contain("New Title 1");
        result.Value.DesiredJobTitles.Should().NotContain("Old Title");
    }

    #endregion

    #region AddSkillAsync Tests

    [Fact(Skip = "InMemory provider doesn't properly handle concurrent entity updates. Use integration tests with real database.")]
    public async Task AddSkillAsync_WithValidRequest_AddsSkill()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var request = new AddSkillRequest
        {
            Name = "C#",
            Level = SkillLevel.Advanced,
            YearsOfExperience = 5
        };

        // Act
        var result = await _sut.AddSkillAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("C#");
        result.Value.Level.Should().Be(SkillLevel.Advanced);
        result.Value.YearsOfExperience.Should().Be(5);
    }

    [Fact]
    public async Task AddSkillAsync_WhenSkillExists_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        profile.Skills.Add(new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C#",
            Level = SkillLevel.Intermediate,
            UserProfileId = profile.Id
        });
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var request = new AddSkillRequest { Name = "C#", Level = SkillLevel.Advanced };

        // Act
        var result = await _sut.AddSkillAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Skill.AlreadyExists");
    }

    #endregion

    #region RemoveSkillAsync Tests

    [Fact]
    public async Task RemoveSkillAsync_WhenSkillExists_RemovesSkill()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C#",
            Level = SkillLevel.Intermediate,
            UserProfileId = profile.Id
        };
        profile.Skills.Add(skill);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RemoveSkillAsync(userId, skill.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProfile = await _dbContext.UserProfiles
            .Include(p => p.Skills)
            .FirstAsync(p => p.UserId == userId);
        updatedProfile.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveSkillAsync_WhenSkillNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RemoveSkillAsync(userId, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Skill.NotFound");
    }

    #endregion

    #region UpdatePreferencesAsync Tests

    [Fact]
    public async Task UpdatePreferencesAsync_CreatesNewPreferences_WhenNoneExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateJobPreferenceRequest
        {
            MinSalary = 50000,
            MaxSalary = 80000,
            IsRemotePreferred = true,
            IsHybridAccepted = true,
            IsOnSiteAccepted = false,
            PreferredEmploymentTypes = EmploymentType.FullTime,
            IsActivelyLooking = true
        };

        // Act
        var result = await _sut.UpdatePreferencesAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinSalary.Should().Be(50000);
        result.Value.MaxSalary.Should().Be(80000);
        result.Value.IsRemotePreferred.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_UpdatesExistingPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        profile.JobPreference = new JobPreference
        {
            Id = Guid.NewGuid(),
            UserProfileId = profile.Id,
            MinSalary = 40000,
            IsActivelyLooking = false
        };
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateJobPreferenceRequest
        {
            MinSalary = 60000,
            IsRemotePreferred = true,
            IsHybridAccepted = true,
            IsOnSiteAccepted = true,
            PreferredEmploymentTypes = EmploymentType.FullTime,
            IsActivelyLooking = true
        };

        // Act
        var result = await _sut.UpdatePreferencesAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinSalary.Should().Be(60000);
        result.Value.IsActivelyLooking.Should().BeTrue();
    }

    #endregion

    #region UploadCVAsync Tests

    [Fact(Skip = "InMemory provider doesn't properly handle concurrent entity updates. Use integration tests with real database.")]
    public async Task UploadCVAsync_WithValidFile_UploadsAndSavesDocument()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        using var stream = new MemoryStream([1, 2, 3, 4]);
        var fileName = "test-cv.pdf";
        var contentType = "application/pdf";
        var storagePath = $"{Guid.NewGuid()}/{fileName}";

        _fileStorage.UploadAsync(stream, fileName, contentType, Arg.Any<CancellationToken>())
            .Returns(storagePath);

        // Act
        var result = await _sut.UploadCVAsync(userId, stream, fileName, contentType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be(fileName);
        result.Value.ContentType.Should().Be(contentType);
        result.Value.IsPrimary.Should().BeTrue();
    }

    [Fact(Skip = "InMemory provider doesn't properly handle concurrent entity updates. Use integration tests with real database.")]
    public async Task UploadCVAsync_SecondCV_IsNotPrimary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var firstCv = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = "first.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            StoragePath = "path/first.pdf",
            IsPrimary = true,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };
        _dbContext.CVDocuments.Add(firstCv);
        await _dbContext.SaveChangesAsync();

        using var stream = new MemoryStream([1, 2, 3, 4]);
        var fileName = "second.pdf";
        var contentType = "application/pdf";

        _fileStorage.UploadAsync(stream, fileName, contentType, Arg.Any<CancellationToken>())
            .Returns($"path/{fileName}");

        // Act
        var result = await _sut.UploadCVAsync(userId, stream, fileName, contentType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region DeleteCVAsync Tests

    [Fact]
    public async Task DeleteCVAsync_WhenDocumentExists_DeletesDocument()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var cvDoc = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            StoragePath = "path/test.pdf",
            IsPrimary = true,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };
        profile.CVDocuments.Add(cvDoc);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteCVAsync(userId, cvDoc.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("path/test.pdf", Arg.Any<CancellationToken>());

        var updatedProfile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .FirstAsync(p => p.UserId == userId);
        updatedProfile.CVDocuments.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCVAsync_WhenPrimaryDeleted_MakesNextDocumentPrimary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var primaryDoc = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = "primary.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            StoragePath = "path/primary.pdf",
            IsPrimary = true,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };
        var secondaryDoc = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = "secondary.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            StoragePath = "path/secondary.pdf",
            IsPrimary = false,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };
        profile.CVDocuments.Add(primaryDoc);
        profile.CVDocuments.Add(secondaryDoc);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteCVAsync(userId, primaryDoc.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProfile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .FirstAsync(p => p.UserId == userId);
        updatedProfile.CVDocuments.Should().HaveCount(1);
        updatedProfile.CVDocuments.First().IsPrimary.Should().BeTrue();
    }

    #endregion

    #region SetPrimaryCVAsync Tests

    [Fact]
    public async Task SetPrimaryCVAsync_SetsNewPrimary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var doc1 = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = "doc1.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            StoragePath = "path/doc1.pdf",
            IsPrimary = true,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };
        var doc2 = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = "doc2.pdf",
            ContentType = "application/pdf",
            FileSize = 100,
            StoragePath = "path/doc2.pdf",
            IsPrimary = false,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };
        profile.CVDocuments.Add(doc1);
        profile.CVDocuments.Add(doc2);
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.SetPrimaryCVAsync(userId, doc2.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProfile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .FirstAsync(p => p.UserId == userId);

        updatedProfile.CVDocuments.Single(d => d.Id == doc1.Id).IsPrimary.Should().BeFalse();
        updatedProfile.CVDocuments.Single(d => d.Id == doc2.Id).IsPrimary.Should().BeTrue();
    }

    #endregion

    private static UserProfile CreateTestProfile(Guid userId)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentJobTitle = "Test Developer",
            Summary = "Test summary",
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
