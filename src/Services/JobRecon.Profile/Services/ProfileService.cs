using JobRecon.Domain.Common;
using JobRecon.Profile.Contracts;
using JobRecon.Profile.Domain;
using JobRecon.Profile.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Profile.Services;

public sealed class ProfileService : IProfileService
{
    private readonly ProfileDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        ProfileDbContext dbContext,
        IFileStorageService fileStorage,
        ILogger<ProfileService> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<ProfileResponse>> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.DesiredJobTitles)
            .Include(p => p.Skills)
            .Include(p => p.JobPreference)
            .Include(p => p.CVDocuments)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<ProfileResponse>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        return Result.Success(MapToResponse(profile));
    }

    public async Task<Result<ProfileResponse>> CreateProfileAsync(
        Guid userId,
        CreateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingProfile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (existingProfile is not null)
        {
            return Result.Failure<ProfileResponse>(Error.Conflict("Profile.AlreadyExists", "Profile already exists for this user"));
        }

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentJobTitle = request.CurrentJobTitle,
            Summary = request.Summary,
            Location = request.Location,
            PhoneNumber = request.PhoneNumber,
            LinkedInUrl = request.LinkedInUrl,
            GitHubUrl = request.GitHubUrl,
            PortfolioUrl = request.PortfolioUrl,
            YearsOfExperience = request.YearsOfExperience,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.DesiredJobTitles is not null)
        {
            foreach (var title in request.DesiredJobTitles)
            {
                profile.DesiredJobTitles.Add(new DesiredJobTitle
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    UserProfileId = profile.Id
                });
            }
        }

        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created profile for user {UserId}", userId);

        return Result.Success(MapToResponse(profile));
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.DesiredJobTitles)
            .Include(p => p.Skills)
            .Include(p => p.JobPreference)
            .Include(p => p.CVDocuments)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<ProfileResponse>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        profile.CurrentJobTitle = request.CurrentJobTitle ?? profile.CurrentJobTitle;
        profile.Summary = request.Summary ?? profile.Summary;
        profile.Location = request.Location ?? profile.Location;
        profile.PhoneNumber = request.PhoneNumber ?? profile.PhoneNumber;
        profile.LinkedInUrl = request.LinkedInUrl ?? profile.LinkedInUrl;
        profile.GitHubUrl = request.GitHubUrl ?? profile.GitHubUrl;
        profile.PortfolioUrl = request.PortfolioUrl ?? profile.PortfolioUrl;
        profile.YearsOfExperience = request.YearsOfExperience ?? profile.YearsOfExperience;
        profile.UpdatedAt = DateTime.UtcNow;

        if (request.DesiredJobTitles is not null)
        {
            _dbContext.DesiredJobTitles.RemoveRange(profile.DesiredJobTitles);
            profile.DesiredJobTitles.Clear();

            foreach (var title in request.DesiredJobTitles)
            {
                var desiredJobTitle = new DesiredJobTitle
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    UserProfileId = profile.Id
                };
                profile.DesiredJobTitles.Add(desiredJobTitle);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated profile for user {UserId}", userId);

        return Result.Success(MapToResponse(profile));
    }

    public async Task<Result<SkillResponse>> AddSkillAsync(
        Guid userId,
        AddSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<SkillResponse>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        var existingSkill = profile.Skills.FirstOrDefault(s =>
            s.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

        if (existingSkill is not null)
        {
            return Result.Failure<SkillResponse>(Error.Conflict("Skill.AlreadyExists", "Skill already exists"));
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Level = request.Level,
            YearsOfExperience = request.YearsOfExperience,
            UserProfileId = profile.Id
        };

        _dbContext.Skills.Add(skill);
        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added skill {SkillName} to profile for user {UserId}", request.Name, userId);

        return Result.Success(new SkillResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Level = skill.Level,
            YearsOfExperience = skill.YearsOfExperience
        });
    }

    public async Task<Result> RemoveSkillAsync(
        Guid userId,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        var skill = profile.Skills.FirstOrDefault(s => s.Id == skillId);

        if (skill is null)
        {
            return Result.Failure(Error.NotFound("Skill.NotFound", "Skill not found"));
        }

        profile.Skills.Remove(skill);
        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed skill {SkillId} from profile for user {UserId}", skillId, userId);

        return Result.Success();
    }

    public async Task<Result<JobPreferenceResponse>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.JobPreference)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<JobPreferenceResponse>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        if (profile.JobPreference is null)
        {
            return Result.Failure<JobPreferenceResponse>(Error.NotFound("JobPreference.NotFound", "Job preferences not set"));
        }

        return Result.Success(MapToPreferenceResponse(profile.JobPreference));
    }

    public async Task<Result<JobPreferenceResponse>> UpdatePreferencesAsync(
        Guid userId,
        UpdateJobPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.JobPreference)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<JobPreferenceResponse>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        if (profile.JobPreference is null)
        {
            var newPreference = new JobPreference
            {
                Id = Guid.NewGuid(),
                UserProfileId = profile.Id
            };
            _dbContext.JobPreferences.Add(newPreference);
            profile.JobPreference = newPreference;
        }

        var pref = profile.JobPreference;
        pref.MinSalary = request.MinSalary;
        pref.MaxSalary = request.MaxSalary;
        pref.PreferredLocations = request.PreferredLocations;
        pref.IsRemotePreferred = request.IsRemotePreferred;
        pref.IsHybridAccepted = request.IsHybridAccepted;
        pref.IsOnSiteAccepted = request.IsOnSiteAccepted;
        pref.PreferredEmploymentType = request.PreferredEmploymentType;
        pref.PreferredIndustries = request.PreferredIndustries;
        pref.ExcludedCompanies = request.ExcludedCompanies;
        pref.IsActivelyLooking = request.IsActivelyLooking;
        pref.AvailableFrom = request.AvailableFrom;
        pref.NoticePeriodDays = request.NoticePeriodDays;

        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated job preferences for user {UserId}", userId);

        return Result.Success(MapToPreferenceResponse(pref));
    }

    public async Task<Result<CVDocumentResponse>> UploadCVAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<CVDocumentResponse>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        var storagePath = await _fileStorage.UploadAsync(fileStream, fileName, contentType, cancellationToken);

        var isFirstCV = !profile.CVDocuments.Any();

        var cvDocument = new CVDocument
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileStream.Length,
            StoragePath = storagePath,
            IsPrimary = isFirstCV,
            IsParsed = false,
            UploadedAt = DateTime.UtcNow,
            UserProfileId = profile.Id
        };

        profile.CVDocuments.Add(cvDocument);
        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded CV {FileName} for user {UserId}", fileName, userId);

        return Result.Success(new CVDocumentResponse
        {
            Id = cvDocument.Id,
            FileName = cvDocument.FileName,
            ContentType = cvDocument.ContentType,
            FileSize = cvDocument.FileSize,
            IsPrimary = cvDocument.IsPrimary,
            IsParsed = cvDocument.IsParsed,
            UploadedAt = cvDocument.UploadedAt
        });
    }

    public async Task<Result<(Stream FileStream, string FileName, string ContentType)>> DownloadCVAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<(Stream, string, string)>(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        var cvDocument = profile.CVDocuments.FirstOrDefault(d => d.Id == documentId);

        if (cvDocument is null)
        {
            return Result.Failure<(Stream, string, string)>(Error.NotFound("CVDocument.NotFound", "CV document not found"));
        }

        var fileStream = await _fileStorage.DownloadAsync(cvDocument.StoragePath, cancellationToken);

        return Result.Success((fileStream, cvDocument.FileName, cvDocument.ContentType));
    }

    public async Task<Result> DeleteCVAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        var cvDocument = profile.CVDocuments.FirstOrDefault(d => d.Id == documentId);

        if (cvDocument is null)
        {
            return Result.Failure(Error.NotFound("CVDocument.NotFound", "CV document not found"));
        }

        await _fileStorage.DeleteAsync(cvDocument.StoragePath, cancellationToken);

        profile.CVDocuments.Remove(cvDocument);
        profile.UpdatedAt = DateTime.UtcNow;

        if (cvDocument.IsPrimary && profile.CVDocuments.Any())
        {
            profile.CVDocuments.First().IsPrimary = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted CV {DocumentId} for user {UserId}", documentId, userId);

        return Result.Success();
    }

    public async Task<Result> SetPrimaryCVAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.CVDocuments)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(Error.NotFound("Profile.NotFound", "Profile not found"));
        }

        var cvDocument = profile.CVDocuments.FirstOrDefault(d => d.Id == documentId);

        if (cvDocument is null)
        {
            return Result.Failure(Error.NotFound("CVDocument.NotFound", "CV document not found"));
        }

        foreach (var doc in profile.CVDocuments)
        {
            doc.IsPrimary = doc.Id == documentId;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set CV {DocumentId} as primary for user {UserId}", documentId, userId);

        return Result.Success();
    }

    private static ProfileResponse MapToResponse(UserProfile profile)
    {
        return new ProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            CurrentJobTitle = profile.CurrentJobTitle,
            Summary = profile.Summary,
            Location = profile.Location,
            PhoneNumber = profile.PhoneNumber,
            LinkedInUrl = profile.LinkedInUrl,
            GitHubUrl = profile.GitHubUrl,
            PortfolioUrl = profile.PortfolioUrl,
            YearsOfExperience = profile.YearsOfExperience,
            DesiredJobTitles = profile.DesiredJobTitles.Select(d => d.Title).ToList(),
            Skills = profile.Skills.Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Level = s.Level,
                YearsOfExperience = s.YearsOfExperience
            }).ToList(),
            JobPreference = profile.JobPreference is not null
                ? MapToPreferenceResponse(profile.JobPreference)
                : null,
            CVDocuments = profile.CVDocuments.Select(d => new CVDocumentResponse
            {
                Id = d.Id,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                IsPrimary = d.IsPrimary,
                IsParsed = d.IsParsed,
                UploadedAt = d.UploadedAt
            }).ToList(),
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static JobPreferenceResponse MapToPreferenceResponse(JobPreference pref)
    {
        return new JobPreferenceResponse
        {
            Id = pref.Id,
            MinSalary = pref.MinSalary,
            MaxSalary = pref.MaxSalary,
            PreferredLocations = pref.PreferredLocations,
            IsRemotePreferred = pref.IsRemotePreferred,
            IsHybridAccepted = pref.IsHybridAccepted,
            IsOnSiteAccepted = pref.IsOnSiteAccepted,
            PreferredEmploymentType = pref.PreferredEmploymentType,
            PreferredIndustries = pref.PreferredIndustries,
            ExcludedCompanies = pref.ExcludedCompanies,
            IsActivelyLooking = pref.IsActivelyLooking,
            AvailableFrom = pref.AvailableFrom,
            NoticePeriodDays = pref.NoticePeriodDays
        };
    }
}
