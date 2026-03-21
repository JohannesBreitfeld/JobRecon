using FluentAssertions;
using JobRecon.Matching.Clients;
using JobRecon.Matching.Contracts;
using JobRecon.Matching.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobRecon.Matching.Tests.Services;

public class MatchingServiceTests
{
    private readonly IProfileClient _profileClient;
    private readonly IJobsClient _jobsClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOllamaClient _ollamaClient;
    private readonly IVectorStore _vectorStore;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MatchingService> _logger;
    private readonly MatchingService _sut;

    public MatchingServiceTests()
    {
        _profileClient = Substitute.For<IProfileClient>();
        _jobsClient = Substitute.For<IJobsClient>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _ollamaClient = Substitute.For<IOllamaClient>();
        _vectorStore = Substitute.For<IVectorStore>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<MatchingService>>();

        // By default, Ollama returns null (triggering heuristic-only fallback)
        _ollamaClient.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((float[]?)null);

        _sut = new MatchingService(_profileClient, _jobsClient, _eventPublisher, _ollamaClient, _vectorStore, _memoryCache, _logger);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithNoProfile_ReturnsEmptyResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns((ProfileDto?)null);

        // Act
        var result = await _sut.GetRecommendationsAsync(userId, new GetRecommendationsRequest());

        // Assert
        result.Recommendations.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithProfileAndJobs_ReturnsMatchedJobs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var jobs = CreateTestJobs();

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetActiveJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new JobListDto(jobs, jobs.Count), new JobListDto([], 0));

        // Act
        var result = await _sut.GetRecommendationsAsync(userId, new GetRecommendationsRequest());

        // Assert
        result.Recommendations.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
        result.Summary.TotalJobsAnalyzed.Should().Be(jobs.Count);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithMatchingSkills_ReturnsHighScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new ProfileDto(
            userId,
            "Software Developer",
            "Experienced developer",
            "Stockholm",
            5,
            [new SkillDto("C#", "Expert", 5), new SkillDto(".NET", "Advanced", 4)],
            [new DesiredJobTitleDto("Software Developer", 1)],
            null);

        var jobs = new List<JobDto>
        {
            new(
                Guid.NewGuid(),
                "Senior .NET Developer",
                "We need a C# developer",
                "Stockholm",
                "Remote",
                "FullTime",
                50000m,
                70000m,
                "SEK",
                "C#, .NET, SQL",
                3,
                7,
                DateTime.UtcNow,
                "https://example.com/job",
                new CompanyDto(Guid.NewGuid(), "Tech Corp", null, "IT"),
                ["C#", ".NET", "SQL"])
        };

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetActiveJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new JobListDto(jobs, 1), new JobListDto([], 0));

        // Act
        var result = await _sut.GetRecommendationsAsync(userId, new GetRecommendationsRequest());

        // Assert
        result.Recommendations.Should().HaveCount(1);
        result.Recommendations[0].MatchScore.Should().BeGreaterThan(0.5);
        result.Recommendations[0].MatchFactors.Should().Contain(f => f.Category == "Skills");
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithExcludedCompany_ReturnsZeroScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new ProfileDto(
            userId,
            "Developer",
            null,
            "Stockholm",
            5,
            [new SkillDto("C#", "Expert", 5)],
            [],
            new JobPreferenceDto(null, null, null, true, true, true, null, null, "Excluded Corp", true));

        var jobs = new List<JobDto>
        {
            new(
                Guid.NewGuid(),
                "Developer",
                "Job at excluded company",
                "Stockholm",
                "Remote",
                "FullTime",
                null,
                null,
                null,
                "C#",
                null,
                null,
                DateTime.UtcNow,
                null,
                new CompanyDto(Guid.NewGuid(), "Excluded Corp", null, null),
                [])
        };

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetActiveJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new JobListDto(jobs, 1), new JobListDto([], 0));

        // Act
        var result = await _sut.GetRecommendationsAsync(userId, new GetRecommendationsRequest());

        // Assert
        result.Recommendations.Should().HaveCount(1);
        result.Recommendations[0].MatchScore.Should().Be(0);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithMinScoreFilter_FiltersLowScores()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var jobs = CreateTestJobs();

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetActiveJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new JobListDto(jobs, jobs.Count), new JobListDto([], 0));

        // Act
        var result = await _sut.GetRecommendationsAsync(userId, new GetRecommendationsRequest(MinScore: 0.8));

        // Assert
        result.Recommendations.Should().OnlyContain(r => r.MatchScore >= 0.8);
    }

    [Fact]
    public async Task GetJobMatchScoreAsync_WithValidJob_ReturnsScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);
        var job = CreateTestJobs().First();
        job = job with { Id = jobId };

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetJobAsync(jobId, Arg.Any<CancellationToken>())
            .Returns(job);

        // Act
        var result = await _sut.GetJobMatchScoreAsync(userId, jobId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.MatchScore.Should().BeGreaterThanOrEqualTo(0);
        result.MatchFactors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetJobMatchScoreAsync_WithNoProfile_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns((ProfileDto?)null);

        // Act
        var result = await _sut.GetJobMatchScoreAsync(userId, jobId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetJobMatchScoreAsync_WithNoJob_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var profile = CreateTestProfile(userId);

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetJobAsync(jobId, Arg.Any<CancellationToken>())
            .Returns((JobDto?)null);

        // Act
        var result = await _sut.GetJobMatchScoreAsync(userId, jobId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithSalaryPreference_MatchesSalaryRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new ProfileDto(
            userId,
            "Developer",
            null,
            "Stockholm",
            5,
            [],
            [],
            new JobPreferenceDto(50000m, 70000m, null, true, true, true, null, null, null, true));

        var jobs = new List<JobDto>
        {
            new(Guid.NewGuid(), "Good Salary Job", null, null, null, null, 55000m, 65000m, "SEK", null, null, null, DateTime.UtcNow, null, new CompanyDto(Guid.NewGuid(), "A", null, null), []),
            new(Guid.NewGuid(), "Low Salary Job", null, null, null, null, 30000m, 40000m, "SEK", null, null, null, DateTime.UtcNow, null, new CompanyDto(Guid.NewGuid(), "B", null, null), [])
        };

        _profileClient.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);
        _jobsClient.GetActiveJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new JobListDto(jobs, 2), new JobListDto([], 0));

        // Act
        var result = await _sut.GetRecommendationsAsync(userId, new GetRecommendationsRequest());

        // Assert
        result.Recommendations.Should().HaveCount(2);
        // The job with matching salary should have higher score
        var goodSalaryJob = result.Recommendations.First(r => r.Title == "Good Salary Job");
        var lowSalaryJob = result.Recommendations.First(r => r.Title == "Low Salary Job");
        goodSalaryJob.MatchFactors.First(f => f.Category == "Salary").Score
            .Should().BeGreaterThan(lowSalaryJob.MatchFactors.First(f => f.Category == "Salary").Score);
    }

    private static ProfileDto CreateTestProfile(Guid userId)
    {
        return new ProfileDto(
            userId,
            "Software Developer",
            "Experienced software developer",
            "Stockholm",
            5,
            [
                new SkillDto("C#", "Expert", 5),
                new SkillDto("JavaScript", "Advanced", 4),
                new SkillDto("SQL", "Intermediate", 3)
            ],
            [
                new DesiredJobTitleDto("Senior Software Developer", 1),
                new DesiredJobTitleDto("Tech Lead", 2)
            ],
            new JobPreferenceDto(
                40000m, 80000m,
                "Stockholm, Gothenburg",
                true, true, false,
                "FullTime, Contract",
                "IT, Tech",
                null,
                true));
    }

    private static List<JobDto> CreateTestJobs()
    {
        return
        [
            new(
                Guid.NewGuid(),
                "Senior Software Developer",
                "Looking for experienced C# developer with SQL skills",
                "Stockholm",
                "Remote",
                "FullTime",
                50000m, 70000m, "SEK",
                "C#, SQL, .NET",
                3, 7,
                DateTime.UtcNow,
                "https://example.com/job1",
                new CompanyDto(Guid.NewGuid(), "Tech Corp", "https://logo.com", "IT"),
                ["C#", ".NET", "SQL", "Backend"]),
            new(
                Guid.NewGuid(),
                "Frontend Developer",
                "Looking for JavaScript developer",
                "Gothenburg",
                "Hybrid",
                "FullTime",
                40000m, 55000m, "SEK",
                "JavaScript, React, CSS",
                2, 5,
                DateTime.UtcNow,
                "https://example.com/job2",
                new CompanyDto(Guid.NewGuid(), "Web Agency", null, "IT"),
                ["JavaScript", "React", "Frontend"]),
            new(
                Guid.NewGuid(),
                "Data Analyst",
                "Analyze data and create reports",
                "Malmö",
                "OnSite",
                "PartTime",
                30000m, 40000m, "SEK",
                "SQL, Excel, Python",
                1, 3,
                DateTime.UtcNow,
                "https://example.com/job3",
                new CompanyDto(Guid.NewGuid(), "Analytics Co", null, "Finance"),
                ["SQL", "Data", "Analytics"])
        ];
    }
}
