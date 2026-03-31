using JobRecon.Matching.Clients;
using JobRecon.Contracts.Events;
using JobRecon.Matching.Contracts;
using JobRecon.Matching.Workers;
namespace JobRecon.Matching.Services;

public sealed class MatchingService : IMatchingService
{
    private readonly IProfileClient _profileClient;
    private readonly IJobsClient _jobsClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOllamaClient _ollamaClient;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<MatchingService> _logger;

    // Heuristic scoring weights (sum to 1.0)
    private const double SkillWeight = 0.30;
    private const double TitleWeight = 0.25;
    private const double LocationWeight = 0.25;
    private const double SalaryWeight = 0.10;
    private const double ExperienceWeight = 0.05;
    private const double EmploymentTypeWeight = 0.05;

    // Blending weights (vector vs heuristic)
    private const double VectorWeight = 0.5;
    private const double HeuristicWeight = 0.5;

    // Vector search pre-filter size
    private const int VectorTopK = 200;

    // Minimum score to trigger notification event
    private const double MinScoreForNotification = 0.5;

    public MatchingService(
        IProfileClient profileClient,
        IJobsClient jobsClient,
        IEventPublisher eventPublisher,
        IOllamaClient ollamaClient,
        IVectorStore vectorStore,
        ILogger<MatchingService> logger)
    {
        _profileClient = profileClient;
        _jobsClient = jobsClient;
        _eventPublisher = eventPublisher;
        _ollamaClient = ollamaClient;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<RecommendationsResponse?> GetRecommendationsAsync(
        Guid userId,
        GetRecommendationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileClient.GetProfileAsync(userId, cancellationToken);
        if (profile == null)
        {
            _logger.LogInformation("No profile found for user {UserId}, skipping recommendations", userId);
            return null;
        }

        // Try vector-based pre-filtering first
        var vectorScores = await GetVectorScoresAsync(profile, cancellationToken);
        var useVectorScoring = vectorScores.Count > 0;

        if (useVectorScoring)
        {
            _logger.LogInformation("Using vector + heuristic matching ({VectorCount} candidates)", vectorScores.Count);
        }
        else
        {
            _logger.LogInformation("Falling back to heuristic-only matching");
        }

        List<JobRecommendation> allRecommendations;
        int totalAnalyzed;

        if (useVectorScoring)
        {
            (allRecommendations, totalAnalyzed) = await MatchWithVectorPreFilterAsync(
                profile, vectorScores, request.MinScore, cancellationToken);
        }
        else
        {
            (allRecommendations, totalAnalyzed) = await MatchHeuristicOnlyAsync(
                profile, request.MinScore, cancellationToken);
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

        // Publish events for high-scoring matches
        foreach (var rec in paginatedRecommendations.Where(r => r.MatchScore >= MinScoreForNotification))
        {
            await PublishMatchEventAsync(userId, rec, cancellationToken);
        }

        var summary = BuildSummary(profile, sortedRecommendations, totalAnalyzed);

        return new RecommendationsResponse(
            paginatedRecommendations,
            sortedRecommendations.Count,
            request.Page,
            request.PageSize,
            summary);
    }

    private async Task<Dictionary<Guid, float>> GetVectorScoresAsync(
        ProfileDto profile, CancellationToken ct)
    {
        try
        {
            var profileText = BuildProfileText(profile);
            var profileEmbedding = await _ollamaClient.GetEmbeddingAsync(profileText, ct);
            if (profileEmbedding is null)
                return new Dictionary<Guid, float>();

            var results = await _vectorStore.SearchAsync(profileEmbedding, VectorTopK, ct);
            return results.ToDictionary(r => r.JobId, r => r.Score);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector search failed, will use heuristic-only matching");
            return new Dictionary<Guid, float>();
        }
    }

    private async Task<(List<JobRecommendation> Recommendations, int TotalAnalyzed)> MatchWithVectorPreFilterAsync(
        ProfileDto profile,
        Dictionary<Guid, float> vectorScores,
        double minScore,
        CancellationToken ct)
    {
        var recommendations = new List<JobRecommendation>();

        // Batch fetch all vector-matched jobs in a single gRPC call
        var jobs = await _jobsClient.GetJobsByIdsAsync(vectorScores.Keys, ct);
        var jobDict = jobs.ToDictionary(j => j.Id);
        var totalAnalyzed = 0;

        foreach (var (jobId, vectorScore) in vectorScores)
        {
            if (!jobDict.TryGetValue(jobId, out var job))
                continue;

            totalAnalyzed++;

            if (IsExcluded(profile, job))
                continue;

            var heuristicScore = CalculateHeuristicScore(profile, job, out var factors);

            // Blend vector and heuristic scores
            var blendedScore = (vectorScore * VectorWeight) + (heuristicScore * HeuristicWeight);

            factors.Add(new MatchFactor("Semantic Similarity", $"Vector score: {vectorScore:P0}", vectorScore, VectorWeight));

            if (blendedScore >= minScore)
            {
                recommendations.Add(CreateRecommendation(job, Math.Round(blendedScore, 2), factors));
            }
        }

        return (recommendations, totalAnalyzed);
    }

    private async Task<(List<JobRecommendation> Recommendations, int TotalAnalyzed)> MatchHeuristicOnlyAsync(
        ProfileDto profile,
        double minScore,
        CancellationToken ct)
    {
        const int batchSize = 100;
        const int maxJobsToScan = 5000;

        _logger.LogWarning("Using heuristic-only matching (vector search unavailable). Performance will be degraded");

        var recommendations = new List<JobRecommendation>();
        var offset = 0;
        var totalAnalyzed = 0;

        while (offset < maxJobsToScan)
        {
            var jobsResponse = await _jobsClient.GetActiveJobsAsync(batchSize, offset, ct);
            if (jobsResponse is null || jobsResponse.Jobs.Count == 0)
                break;

            foreach (var job in jobsResponse.Jobs)
            {
                totalAnalyzed++;

                if (IsExcluded(profile, job))
                    continue;

                var score = CalculateHeuristicScore(profile, job, out var factors);

                if (score >= minScore)
                {
                    recommendations.Add(CreateRecommendation(job, Math.Round(score, 2), factors));
                }
            }

            offset += batchSize;
        }

        return (recommendations, totalAnalyzed);
    }

    private static string BuildProfileText(ProfileDto profile)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(profile.CurrentJobTitle))
            parts.Add(profile.CurrentJobTitle);

        if (!string.IsNullOrWhiteSpace(profile.Summary))
            parts.Add(profile.Summary);

        if (profile.Skills.Count > 0)
            parts.Add($"Skills: {string.Join(", ", profile.Skills.Select(s => s.Name))}");

        if (profile.DesiredJobTitles.Count > 0)
            parts.Add($"Looking for: {string.Join(", ", profile.DesiredJobTitles.Select(t => t.Title))}");

        if (!string.IsNullOrWhiteSpace(profile.Location))
            parts.Add($"Location: {profile.Location}");

        return string.Join(". ", parts);
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

        var heuristicScore = CalculateHeuristicScore(profile, job, out var factors);

        // Try to get vector score for this specific job
        var vectorScores = await GetVectorScoresAsync(profile, cancellationToken);
        if (vectorScores.TryGetValue(jobId, out var vectorScore))
        {
            var blendedScore = (vectorScore * VectorWeight) + (heuristicScore * HeuristicWeight);
            factors.Add(new MatchFactor("Semantic Similarity", $"Vector score: {vectorScore:P0}", vectorScore, VectorWeight));

            if (IsExcluded(profile, job))
                blendedScore = 0;

            return CreateRecommendation(job, Math.Round(blendedScore, 2), factors);
        }

        if (IsExcluded(profile, job))
            heuristicScore = 0;

        return CreateRecommendation(job, Math.Round(heuristicScore, 2), factors);
    }

    private static double CalculateHeuristicScore(ProfileDto profile, JobDto job, out List<MatchFactor> factors)
    {
        factors = [];

        var skillScore = CalculateSkillScore(profile, job, out var skillDescription);
        factors.Add(new MatchFactor("Skills", skillDescription, skillScore, SkillWeight));

        var titleScore = CalculateTitleScore(profile, job, out var titleDescription);
        factors.Add(new MatchFactor("Job Title", titleDescription, titleScore, TitleWeight));

        var locationScore = CalculateLocationScore(profile, job, out var locationDescription);
        factors.Add(new MatchFactor("Location", locationDescription, locationScore, LocationWeight));

        var salaryScore = CalculateSalaryScore(profile, job, out var salaryDescription);
        factors.Add(new MatchFactor("Salary", salaryDescription, salaryScore, SalaryWeight));

        var experienceScore = CalculateExperienceScore(profile, job, out var experienceDescription);
        factors.Add(new MatchFactor("Experience", experienceDescription, experienceScore, ExperienceWeight));

        var employmentScore = CalculateEmploymentTypeScore(profile, job, out var employmentDescription);
        factors.Add(new MatchFactor("Employment Type", employmentDescription, employmentScore, EmploymentTypeWeight));

        return factors.Sum(f => f.Score * f.Weight);
    }

    private static JobRecommendation CreateRecommendation(JobDto job, double score, List<MatchFactor> factors)
    {
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
            score,
            factors);
    }

    private static double CalculateSkillScore(ProfileDto profile, JobDto job, out string description)
    {
        if (profile.Skills.Count == 0)
        {
            description = "No skills in profile";
            return 1.0;
        }

        var userSkills = profile.Skills
            .Select(s => s.Name.ToLowerInvariant().Trim())
            .ToHashSet();

        var jobSkills = (job.RequiredSkills ?? "")
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
            description = "Job has no specified skills — cannot verify match";
            return 0.3;
        }

        // Fast path: exact or substring match (covers most cases)
        var matchedSkills = new List<string>();
        var unmatchedJobSkills = new List<string>();

        foreach (var js in allJobSkills)
        {
            if (userSkills.Contains(js) || userSkills.Any(us => us.Contains(js) || js.Contains(us)))
            {
                matchedSkills.Add(js);
            }
            else
            {
                unmatchedJobSkills.Add(js);
            }
        }

        // Slow path: Levenshtein only for remaining unmatched skills
        foreach (var js in unmatchedJobSkills)
        {
            if (userSkills.Any(us => LevenshteinSimilarity(us, js) > 0.8))
            {
                matchedSkills.Add(js);
            }
        }

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

        // No title data on either side — neutral
        if (string.IsNullOrEmpty(profile.CurrentJobTitle) && profile.DesiredJobTitles.Count == 0)
        {
            description = "No title set in profile";
            return 1.0;
        }

        description = "No title match found";
        return 0.3;
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

        // Check preferred locations — applies regardless of whether job has a location
        if (prefs != null && !string.IsNullOrEmpty(prefs.PreferredLocations))
        {
            if (string.IsNullOrEmpty(job.Location))
            {
                description = "Job has no location — cannot verify against preferred areas";
                return 0.3;
            }

            var jobLocation = job.Location.ToLowerInvariant();
            var preferredLocations = prefs.PreferredLocations
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().ToLowerInvariant())
                .ToList();

            if (preferredLocations.Any(pl => jobLocation.Contains(pl) || pl.Contains(jobLocation)))
            {
                description = $"Location matches preference: {job.Location}";
                return 0.9;
            }

            description = $"Location ({job.Location}) outside preferred areas";
            return 0.2;
        }

        // No preferred locations — use current location as a soft signal if available
        if (!string.IsNullOrEmpty(profile.Location) && !string.IsNullOrEmpty(job.Location))
        {
            var userLocation = profile.Location.ToLowerInvariant();
            var jobLocation = job.Location.ToLowerInvariant();
            if (jobLocation.Contains(userLocation) || userLocation.Contains(jobLocation))
            {
                description = $"Near your location: {job.Location}";
                return 0.8;
            }
        }

        description = "No location data to match on";
        return 1.0;
    }

    private static double CalculateSalaryScore(ProfileDto profile, JobDto job, out string description)
    {
        var prefs = profile.Preferences;

        if (prefs == null || (!prefs.MinSalary.HasValue && !prefs.MaxSalary.HasValue))
        {
            description = "No salary preference set";
            return 1.0;
        }

        if (!job.SalaryMin.HasValue && !job.SalaryMax.HasValue)
        {
            description = "Job salary not specified";
            return 1.0;
        }

        var userMin = prefs.MinSalary ?? 0;
        var userMax = prefs.MaxSalary ?? decimal.MaxValue;
        var jobMin = job.SalaryMin ?? 0;
        var jobMax = job.SalaryMax ?? job.SalaryMin ?? 0;

        // Salary ranges overlap — strong positive signal
        if (jobMax >= userMin && jobMin <= userMax)
        {
            description = $"Salary matches: {job.SalaryMin:N0}-{job.SalaryMax:N0} {job.SalaryCurrency}";
            return 1.0;
        }

        // Job pays above user's max — still fine
        if (jobMin > userMax)
        {
            description = $"Salary above expected range ({job.SalaryMin:N0} > {userMax:N0})";
            return 1.0;
        }

        // Job pays below user's minimum — hard mismatch
        description = $"Salary too low ({job.SalaryMax:N0} < {userMin:N0})";
        return 0.1;
    }

    private static double CalculateExperienceScore(ProfileDto profile, JobDto job, out string description)
    {
        if (!profile.YearsOfExperience.HasValue)
        {
            description = "Experience not specified in profile";
            return 1.0;
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
            return 1.0;
        }

        if (string.IsNullOrEmpty(job.EmploymentType))
        {
            description = "Job employment type not specified — cannot verify match";
            return 0.3;
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

    private async Task PublishMatchEventAsync(
        Guid userId,
        JobRecommendation recommendation,
        CancellationToken ct)
    {
        try
        {
            var topFactors = recommendation.MatchFactors
                .OrderByDescending(f => f.Score * f.Weight)
                .Take(3)
                .Select(f => new MatchFactorEvent(f.Category, f.Score, f.Description))
                .ToList();

            var eventData = new JobMatchedEvent(
                EventId: Guid.NewGuid(),
                UserId: userId,
                JobId: recommendation.JobId,
                JobTitle: recommendation.Title,
                CompanyName: recommendation.CompanyName,
                Location: recommendation.Location,
                MatchScore: recommendation.MatchScore,
                TopFactors: topFactors,
                JobUrl: recommendation.ExternalUrl,
                MatchedAt: DateTime.UtcNow);

            await _eventPublisher.PublishJobMatchedAsync(eventData, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish match event for job {JobId}", recommendation.JobId);
        }
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
