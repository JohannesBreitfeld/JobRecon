using FluentAssertions;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Jobs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobRecon.Jobs.Tests.Services;

public class JobServiceTests : IDisposable
{
    private readonly JobsDbContext _dbContext;
    private readonly JobService _sut;

    public JobServiceTests()
    {
        var options = new DbContextOptionsBuilder<JobsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new JobsDbContext(options);
        var logger = Substitute.For<ILogger<JobService>>();
        _sut = new JobService(_dbContext, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private async Task<(Company company, JobSource source)> CreateTestDataAsync()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            NormalizedName = "test company"
        };

        var source = new JobSource
        {
            Id = Guid.NewGuid(),
            Name = "Manual",
            Type = JobSourceType.Manual,
            IsEnabled = true
        };

        _dbContext.Companies.Add(company);
        _dbContext.JobSources.Add(source);
        await _dbContext.SaveChangesAsync();

        return (company, source);
    }

    private async Task<Job> CreateTestJobAsync(Company company, JobSource source, string title = "Software Developer")
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = title,
            NormalizedTitle = title.ToLower(),
            Description = "Test job description",
            Location = "Stockholm",
            WorkLocationType = WorkLocationType.Remote,
            EmploymentType = EmploymentType.FullTime,
            SalaryMin = 50000,
            SalaryMax = 70000,
            SalaryCurrency = "SEK",
            Status = JobStatus.Active,
            PostedAt = DateTime.UtcNow,
            CompanyId = company.Id,
            Company = company,
            JobSourceId = source.Id,
            JobSource = source
        };

        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync();

        return job;
    }

    [Fact]
    public async Task SearchJobsAsync_WithNoFilters_ReturnsAllActiveJobs()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        await CreateTestJobAsync(company, source, "Job 1");
        await CreateTestJobAsync(company, source, "Job 2");
        await CreateTestJobAsync(company, source, "Job 3");

        var request = new JobSearchRequest { Page = 1, PageSize = 20 };

        // Act
        var result = await _sut.SearchJobsAsync(null, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchJobsAsync_WithQuery_ReturnsMatchingJobs()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        await CreateTestJobAsync(company, source, "React Developer");
        await CreateTestJobAsync(company, source, "Java Developer");
        await CreateTestJobAsync(company, source, "Project Manager");

        var request = new JobSearchRequest { Query = "developer", Page = 1, PageSize = 20 };

        // Act
        var result = await _sut.SearchJobsAsync(null, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchJobsAsync_WithLocation_ReturnsMatchingJobs()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job1 = await CreateTestJobAsync(company, source, "Job 1");
        job1.Location = "Stockholm";
        var job2 = await CreateTestJobAsync(company, source, "Job 2");
        job2.Location = "Göteborg";
        await _dbContext.SaveChangesAsync();

        var request = new JobSearchRequest { Location = "Stockholm", Page = 1, PageSize = 20 };

        // Act
        var result = await _sut.SearchJobsAsync(null, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(1);
        result.Value.Jobs.First().Location.Should().Be("Stockholm");
    }

    [Fact]
    public async Task SearchJobsAsync_WithWorkLocationType_ReturnsMatchingJobs()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job1 = await CreateTestJobAsync(company, source, "Remote Job");
        job1.WorkLocationType = WorkLocationType.Remote;
        var job2 = await CreateTestJobAsync(company, source, "OnSite Job");
        job2.WorkLocationType = WorkLocationType.OnSite;
        await _dbContext.SaveChangesAsync();

        var request = new JobSearchRequest { WorkLocationType = WorkLocationType.Remote, Page = 1, PageSize = 20 };

        // Act
        var result = await _sut.SearchJobsAsync(null, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(1);
        result.Value.Jobs.First().WorkLocationType.Should().Be(WorkLocationType.Remote);
    }

    [Fact]
    public async Task SearchJobsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        for (int i = 0; i < 25; i++)
        {
            await CreateTestJobAsync(company, source, $"Job {i}");
        }

        var request = new JobSearchRequest { Page = 2, PageSize = 10 };

        // Act
        var result = await _sut.SearchJobsAsync(null, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Jobs.Should().HaveCount(10);
        result.Value.Page.Should().Be(2);
        result.Value.TotalCount.Should().Be(25);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetJobAsync_WithValidId_ReturnsJob()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job = await CreateTestJobAsync(company, source);

        // Act
        var result = await _sut.GetJobAsync(job.Id, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(job.Id);
        result.Value.Title.Should().Be(job.Title);
        result.Value.Company.Name.Should().Be(company.Name);
    }

    [Fact]
    public async Task GetJobAsync_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _sut.GetJobAsync(invalidId, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Job.NotFound");
    }

    [Fact]
    public async Task CreateJobAsync_WithValidRequest_CreatesJob()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Title = "New Job",
            Description = "Job description",
            CompanyName = "New Company",
            Location = "Malmö",
            EmploymentType = EmploymentType.FullTime
        };

        // Act
        var result = await _sut.CreateJobAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("New Job");
        result.Value.Company.Name.Should().Be("New Company");

        var jobInDb = await _dbContext.Jobs.FirstOrDefaultAsync(j => j.Id == result.Value.Id);
        jobInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateJobAsync_WithExistingCompany_ReusesCompany()
    {
        // Arrange
        var (company, _) = await CreateTestDataAsync();

        var request = new CreateJobRequest
        {
            Title = "New Job",
            CompanyName = "Test Company"
        };

        // Act
        var result = await _sut.CreateJobAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Company.Id.Should().Be(company.Id);

        var companyCount = await _dbContext.Companies.CountAsync();
        companyCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateJobAsync_WithTags_CreatesTags()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Title = "Developer Job",
            CompanyName = "Tech Company",
            Tags = ["C#", "React", "Azure"]
        };

        // Act
        var result = await _sut.CreateJobAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().HaveCount(3);
        result.Value.Tags.Should().Contain("C#");
    }

    [Fact]
    public async Task SaveJobAsync_WithValidJob_SavesJob()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job = await CreateTestJobAsync(company, source);
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.SaveJobAsync(userId, job.Id, new SaveJobRequest { Notes = "Interesting job" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SavedJobStatus.Saved);
        result.Value.Notes.Should().Be("Interesting job");

        var savedInDb = await _dbContext.SavedJobs.FirstOrDefaultAsync(s => s.UserId == userId);
        savedInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveJobAsync_WhenAlreadySaved_ReturnsConflict()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job = await CreateTestJobAsync(company, source);
        var userId = Guid.NewGuid();

        await _sut.SaveJobAsync(userId, job.Id, new SaveJobRequest());

        // Act
        var result = await _sut.SaveJobAsync(userId, job.Id, new SaveJobRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SavedJob.AlreadyExists");
    }

    [Fact]
    public async Task SaveJobAsync_WithInvalidJobId_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidJobId = Guid.NewGuid();

        // Act
        var result = await _sut.SaveJobAsync(userId, invalidJobId, new SaveJobRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Job.NotFound");
    }

    [Fact]
    public async Task GetSavedJobsAsync_ReturnsUserSavedJobs()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job1 = await CreateTestJobAsync(company, source, "Job 1");
        var job2 = await CreateTestJobAsync(company, source, "Job 2");
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await _sut.SaveJobAsync(userId, job1.Id, new SaveJobRequest());
        await _sut.SaveJobAsync(userId, job2.Id, new SaveJobRequest());
        await _sut.SaveJobAsync(otherUserId, job1.Id, new SaveJobRequest());

        // Act
        var result = await _sut.GetSavedJobsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateSavedJobAsync_UpdatesStatus()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job = await CreateTestJobAsync(company, source);
        var userId = Guid.NewGuid();

        await _sut.SaveJobAsync(userId, job.Id, new SaveJobRequest());

        // Act
        var result = await _sut.UpdateSavedJobAsync(userId, job.Id, new UpdateSavedJobRequest
        {
            Status = SavedJobStatus.Applied,
            Notes = "Applied today"
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SavedJobStatus.Applied);
        result.Value.Notes.Should().Be("Applied today");
    }

    [Fact]
    public async Task RemoveSavedJobAsync_RemovesSavedJob()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        var job = await CreateTestJobAsync(company, source);
        var userId = Guid.NewGuid();

        await _sut.SaveJobAsync(userId, job.Id, new SaveJobRequest());

        // Act
        var result = await _sut.RemoveSavedJobAsync(userId, job.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var savedInDb = await _dbContext.SavedJobs.FirstOrDefaultAsync(s => s.UserId == userId);
        savedInDb.Should().BeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        await CreateTestJobAsync(company, source, "Job 1");
        await CreateTestJobAsync(company, source, "Job 2");
        await CreateTestJobAsync(company, source, "Job 3");

        // Act
        var result = await _sut.GetStatisticsAsync(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalJobs.Should().Be(3);
        result.Value.ActiveJobs.Should().Be(3);
    }

    [Fact]
    public async Task GetCompaniesAsync_ReturnsCompanies()
    {
        // Arrange
        var (company, source) = await CreateTestDataAsync();
        await CreateTestJobAsync(company, source);

        // Act
        var result = await _sut.GetCompaniesAsync(null, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("Test Company");
    }

    [Fact]
    public async Task GetCompaniesAsync_WithSearch_ReturnsMatchingCompanies()
    {
        // Arrange
        var company1 = new Company { Id = Guid.NewGuid(), Name = "Tech Corp", NormalizedName = "tech corp" };
        var company2 = new Company { Id = Guid.NewGuid(), Name = "Finance Inc", NormalizedName = "finance inc" };
        _dbContext.Companies.AddRange(company1, company2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetCompaniesAsync("tech", 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("Tech Corp");
    }
}
