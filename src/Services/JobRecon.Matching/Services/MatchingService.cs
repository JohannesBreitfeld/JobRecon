using JobRecon.Matching.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace JobRecon.Matching.Services;

public sealed class MatchingService : IMatchingService
{
    private readonly IProfileClient _profileClient;
    private readonly IJobsClient _jobsClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MatchingService> _logger;

    // Scoring weights (sum to 1.0)
    private const double SkillWeight = 0.30;
    private const double TitleWeight = 0.25;
    private const double LocationWeight = 0.15;
    private const double SalaryWeight = 0.15;
    private const double ExperienceWeight = 0.10;
    private const double EmploymentTypeWeight = 0.05;

    public MatchingService(
        IProfileClient profileClient,
        IJobsClient jobsClient,
        IMemoryCache cache,
        ILogger<MatchingService> logger)
    {
        _profileClient = profileClient;
        _jobsClient = jobsClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RecommendationsResponse> GetRecommendationsAsync(
        Guid userId,
        GetRecommendationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileClient.GetProfileAsync(userId, cancellationToken);
        if (profile == null)
        {
            _logger.LogWarning("Profile not found for user {UserId}", userId);
            return new RecommendationsResponse([], 0, request.Page, request.PageSize,
                new MatchingSummary(0, 0, 0, [], []));
        }

        // Fetch active jobs (batch processing)
        var allRecommendations = new List<JobRecommendation>();
        var offset = 0;
        const int batchSize = 100;
        var totalAnalyzed = 0;

        while (true)
        {
            var jobsResponse = await _jobsClient.GetActiveJobsAsync(batchSize, offset, cancellationToken);
            if (jobsResponse == null || jobsResponse.Jobs.Count == 0)
                break;

            foreach (var job in jobsResponse.Jobs)
            {
                totalAnalyzed++;
                var recommendation = CalculateMatch(profile, job);

                if (recommendation.MatchScore >= request.MinScore)
                {
                    allRecommendations.Add(recommendation);
                }
            }

            offset += batchSize;

            // Limit total jobs analyzed for performance
            if (offset >= 5000)
                break;
        }

        // Sort by match score descending
        var sortedRecommendations = allRecommendations
            .OrderByDescending(r => r.MatchScore)
            .ToList();

        // Paginate
        var paginatedRecommendations = sortedRecommendations
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Build summary
        var summary = BuildSummary(profile, sortedRecommendations, totalAnalyzed);

        return new RecommendationsResponse(
            paginatedRecommendations,
            sortedRecommendations.Count,
            request.Page,
            request.PageSize,
            summary);
    }

    public async Task<JobRecommendation?> GetJobMatchScoreAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileClient.GetProfileAsync(userId, cancellationToken);
        if (profile == null)
            return null;

        var job = await _jobsClient.GetJobAsync(jobId, cancellationToken);
        if (job == null)
            return null;

        return CalculateMatch(profile, job);
    }

    private JobRecommendation CalculateMatch(ProfileDto profile, JobDto job)
    {
        var factors = new List<MatchFactor>();

        // 1. Skill matching
        var skillScore = CalculateSkillScore(profile, job, out var skillDescription);
        factors.Add(new MatchFactor("Skills", skillDescription, skillScore, SkillWeight));

        // 2. Title matching
        var titleScore = CalculateTitleScore(profile, job, out var titleDescription);
        factors.Add(new MatchFactor("Job Title", titleDescription, titleScore, TitleWeight));

        // 3. Location matching
        var locationScore = CalculateLocationScore(profile, job, out var locationDescription);
        factors.Add(new MatchFactor("Location", locationDescription, locationScore, LocationWeight));

        // 4. Salary matching
        var salaryScore = CalculateSalaryScore(profile, job, out var salaryDescription);
        factors.Add(new MatchFactor("Salary", salaryDescription, salaryScore, SalaryWeight));

        // 5. Experience matching
        var experienceScore = CalculateExperienceScore(profile, job, out var experienceDescription);
        factors.Add(new MatchFactor("Experience", experienceDescription, experienceScore, ExperienceWeight));

        // 6. Employment type matching
        var employmentScore = CalculateEmploymentTypeScore(profile, job, out var employmentDescription);
        factors.Add(new MatchFactor("Employment Type", employmentDescription, employmentScore, EmploymentTypeWeight));

        // Calculate weighted total
        var totalScore = factors.Sum(f => f.Score * f.Weight);

        // Check for exclusions (excluded companies, wrong work location type)
        if (IsExcluded(profile, job))
        {
            totalScore = 0;
        }

        return new JobRecommendation(
            job.Id,
            job.Title,
            job.Company.Name,
            job.Company.LogoUrl,
            job.Location,
            job.WorkLocationType,
            job.EmploymentType,
            job.SalaryMin,
            job.SalaryMax,
            job.SalaryCurrency,
            job.PostedAt,
            job.ExternalUrl,
            Math.Round(totalScore, 2),
            factors);
    }

    private static double CalculateSkillScore(ProfileDto profile, JobDto job, out string description)
    {
        if (profile.Skills.Count == 0 || string.IsNullOrEmpty(job.RequiredSkills))
        {
            description = "No skill data available";
            return 0.5; // Neutral score
        }

        var userSkills = profile.Skills
            .Select(s => s.Name.ToLowerInvariant().Trim())
            .ToHashSet();

        var jobSkills = job.RequiredSkills
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.ToLowerInvariant().Trim())
            .ToList();

        // Also check tags
        var jobTags = job.Tags
            .Select(t => t.ToLowerInvariant().Trim())
            .ToList();

        var allJobSkills = jobSkills.Concat(jobTags).Distinct().ToList();

        if (allJobSkills.Count == 0)
        {
            description = "Job has no specified skills";
            return 0.5;
        }

        var matchedSkills = allJobSkills
            .Where(js => userSkills.Any(us =>
                us.Contains(js) || js.Contains(us) ||
                LevenshteinSimilarity(us, js) > 0.8))
            .ToList();

        var matchRatio = (double)matchedSkills.Count / allJobSkills.Count;

        // Bonus for expert-level matching skills
        var expertBonus = profile.Skills
            .Where(s => s.Level == "Expert" || s.Level == "Advanced")
            .Any(s => matchedSkills.Any(ms =>
                ms.Contains(s.Name.ToLowerInvariant()) ||
                s.Name.ToLowerInvariant().Contains(ms)))
            ? 0.1 : 0;

        var score = Math.Min(1.0, matchRatio + expertBonus);

        description = matchedSkills.Count > 0
            ? $"Matched {matchedSkills.Count}/{allJobSkills.Count} skills"
            : "No matching skills found";

        return score;
    }

    private static double CalculateTitleScore(ProfileDto profile, JobDto job, out string description)
    {
        var jobTitle = job.Title.ToLowerInvariant();

        // Check desired job titles
        if (profile.DesiredJobTitles.Count > 0)
        {
            var bestMatch = profile.DesiredJobTitles
                .Select(dt => new
                {
                    Title = dt.Title,
                    Priority = dt.Priority,
                    Similarity = CalculateTitleSimilarity(dt.Title.ToLowerInvariant(), jobTitle)
                })
                .OrderByDescending(x => x.Similarity)
                .ThenBy(x => x.Priority)
                .FirstOrDefault();

            if (bestMatch != null && bestMatch.Similarity > 0.3)
            {
                description = $"Matches desired title: {bestMatch.Title}";
                return bestMatch.Similarity;
            }
        }

        // Check current job title
        if (!string.IsNullOrEmpty(profile.CurrentJobTitle))
        {
            var similarity = CalculateTitleSimilarity(profile.CurrentJobTitle.ToLowerInvariant(), jobTitle);
            if (similarity > 0.3)
            {
                description = $"Similar to current role: {profile.CurrentJobTitle}";
                return similarity * 0.8; // Slightly lower weight for current title
            }
        }

        description = "No title match found";
        return 0.3; // Base score for unmatched
    }

    private static double CalculateTitleSimilarity(string title1, string title2)
    {
        // Exact match
        if (title1 == title2) return 1.0;

        // Contains match
        if (title1.Contains(title2) || title2.Contains(title1)) return 0.9;

        // Word overlap
        var words1 = title1.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = title2.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        if (union == 0) return 0;

        return (double)intersection / union;
    }

    private static double CalculateLocationScore(ProfileDto profile, JobDto job, out string description)
    {
        var prefs = profile.Preferences;

        // Check work location type preferences
        if (prefs != null && !string.IsNullOrEmpty(job.WorkLocationType))
        {
            var workType = job.WorkLocationType.ToLowerInvariant();

            if (workType == "remote" && prefs.IsRemotePreferred)
            {
                description = "Remote work - preferred";
                return 1.0;
            }

            if (workType == "hybrid" && prefs.IsHybridAccepted)
            {
                description = "Hybrid work - accepted";
                return 0.8;
            }

            if (workType == "onsite" && prefs.IsOnSiteAccepted)
            {
                description = "On-site work - accepted";
                return 0.7;
            }

            // Work type not accepted
            if ((workType == "remote" && !prefs.IsRemotePreferred) ||
                (workType == "hybrid" && !prefs.IsHybridAccepted) ||
                (workType == "onsite" && !prefs.IsOnSiteAccepted))
            {
                description = $"Work type ({job.WorkLocationType}) not preferred";
                return 0.2;
            }
        }

        // Check location match
        if (!string.IsNullOrEmpty(job.Location))
        {
            var jobLocation = job.Location.ToLowerInvariant();

            // Check preferred locations
            if (prefs != null && !string.IsNullOrEmpty(prefs.PreferredLocations))
            {
                var preferredLocations = prefs.PreferredLocations
                    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().ToLowerInvariant())
                    .ToList();

                if (preferredLocations.Any(pl => jobLocation.Contains(pl) || pl.Contains(jobLocation)))
                {
                    description = $"Location matches preference: {job.Location}";
                    return 0.9;
                }
            }

            // Check user's current location
            if (!string.IsNullOrEmpty(profile.Location))
            {
                var userLocation = profile.Location.ToLowerInvariant();
                if (jobLocation.Contains(userLocation) || userLocation.Contains(jobLocation))
                {
                    description = $"Near your location: {job.Location}";
                    return 0.8;
                }
            }
        }

        description = "Location not specified or no match";
        return 0.5;
    }

    private static double CalculateSalaryScore(ProfileDto profile, JobDto job, out string description)
    {
        var prefs = profile.Preferences;

        if (prefs == null || (!prefs.MinSalary.HasValue && !prefs.MaxSalary.HasValue))
        {
            description = "No salary preference set";
            return 0.5;
        }

        if (!job.SalaryMin.HasValue && !job.SalaryMax.HasValue)
        {
            description = "Job salary not specified";
            return 0.5;
        }

        var userMin = prefs.MinSalary ?? 0;
        var userMax = prefs.MaxSalary ?? decimal.MaxValue;
        var jobMin = job.SalaryMin ?? 0;
        var jobMax = job.SalaryMax ?? job.SalaryMin ?? 0;

        // Perfect match: job salary range overlaps with user preference
        if (jobMax >= userMin && jobMin <= userMax)
        {
            var overlap = Math.Min(jobMax, userMax) - Math.Max(jobMin, userMin);
            var userRange = userMax - userMin;

            if (userRange > 0 && userRange < decimal.MaxValue)
            {
                var overlapRatio = overlap / userRange;
                description = $"Salary range matches: {job.SalaryMin:N0}-{job.SalaryMax:N0} {job.SalaryCurrency}";
                return Math.Min(1.0, 0.6 + (double)overlapRatio * 0.4);
            }

            description = $"Salary in range: {job.SalaryMin:N0}-{job.SalaryMax:N0} {job.SalaryCurrency}";
            return 0.8;
        }

        // Below minimum
        if (jobMax < userMin)
        {
            var gap = (userMin - jobMax) / userMin;
            description = $"Below salary expectation ({job.SalaryMax:N0} < {userMin:N0})";
            return Math.Max(0.1, 0.5 - (double)gap);
        }

        description = "Salary above expected range";
        return 0.9; // Above max is usually fine
    }

    private static double CalculateExperienceScore(ProfileDto profile, JobDto job, out string description)
    {
        if (!profile.YearsOfExperience.HasValue)
        {
            description = "Experience not specified in profile";
            return 0.5;
        }

        var userYears = profile.YearsOfExperience.Value;

        if (!job.ExperienceYearsMin.HasValue && !job.ExperienceYearsMax.HasValue)
        {
            description = "No experience requirement specified";
            return 0.7; // Slightly positive
        }

        var jobMin = job.ExperienceYearsMin ?? 0;
        var jobMax = job.ExperienceYearsMax ?? int.MaxValue;

        if (userYears >= jobMin && userYears <= jobMax)
        {
            description = $"Experience matches ({userYears} years)";
            return 1.0;
        }

        if (userYears > jobMax)
        {
            description = $"Overqualified ({userYears} > {jobMax} years)";
            return 0.6; // Might still be interested
        }

        // Under-qualified
        var gap = jobMin - userYears;
        if (gap <= 2)
        {
            description = $"Slightly under experience requirement ({userYears}/{jobMin} years)";
            return 0.5;
        }

        description = $"Under experience requirement ({userYears}/{jobMin} years)";
        return 0.2;
    }

    private static double CalculateEmploymentTypeScore(ProfileDto profile, JobDto job, out string description)
    {
        var prefs = profile.Preferences;

        if (prefs == null || string.IsNullOrEmpty(prefs.PreferredEmploymentTypes))
        {
            description = "No employment type preference";
            return 0.7;
        }

        if (string.IsNullOrEmpty(job.EmploymentType))
        {
            description = "Job employment type not specified";
            return 0.5;
        }

        var preferredTypes = prefs.PreferredEmploymentTypes
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .ToHashSet();

        var jobType = job.EmploymentType.ToLowerInvariant();

        if (preferredTypes.Contains(jobType) ||
            preferredTypes.Any(pt => jobType.Contains(pt) || pt.Contains(jobType)))
        {
            description = $"Employment type matches: {job.EmploymentType}";
            return 1.0;
        }

        description = $"Employment type ({job.EmploymentType}) not preferred";
        return 0.3;
    }

    private static bool IsExcluded(ProfileDto profile, JobDto job)
    {
        var prefs = profile.Preferences;
        if (prefs == null) return false;

        // Check excluded companies
        if (!string.IsNullOrEmpty(prefs.ExcludedCompanies))
        {
            var excluded = prefs.ExcludedCompanies
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToLowerInvariant())
                .ToHashSet();

            var companyName = job.Company.Name.ToLowerInvariant();
            if (excluded.Any(e => companyName.Contains(e) || e.Contains(companyName)))
            {
                return true;
            }
        }

        return false;
    }

    private static MatchingSummary BuildSummary(
        ProfileDto profile,
        List<JobRecommendation> recommendations,
        int totalAnalyzed)
    {
        var avgScore = recommendations.Count > 0
            ? recommendations.Average(r => r.MatchScore)
            : 0;

        var topSkills = profile.Skills
            .OrderByDescending(s => s.Level switch
            {
                "Expert" => 4,
                "Advanced" => 3,
                "Intermediate" => 2,
                _ => 1
            })
            .Take(5)
            .Select(s => s.Name)
            .ToList();

        var topTitles = profile.DesiredJobTitles
            .OrderBy(t => t.Priority)
            .Take(3)
            .Select(t => t.Title)
            .ToList();

        return new MatchingSummary(
            totalAnalyzed,
            recommendations.Count,
            Math.Round(avgScore, 2),
            topSkills,
            topTitles);
    }

    private static double LevenshteinSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        var maxLen = Math.Max(s1.Length, s2.Length);
        if (maxLen == 0) return 1;

        var distance = LevenshteinDistance(s1, s2);
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var dp = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) dp[i, 0] = i;
        for (var j = 0; j <= n; j++) dp[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[m, n];
    }
}
