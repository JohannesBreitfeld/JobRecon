using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JobRecon.Jobs.Contracts;

namespace JobRecon.Jobs.Services;

public static class JobCacheKeys
{
    public static string Tags(string? search, int limit)
        => $"tags:{search?.ToLower() ?? ""}:{limit}";

    public static string StatisticsGlobal()
        => "stats:global";

    public static string Search(JobSearchRequest request)
    {
        var normalized = new SortedDictionary<string, string?>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(request.Query)) normalized["q"] = request.Query.ToLower();
        if (!string.IsNullOrWhiteSpace(request.Location)) normalized["loc"] = request.Location.ToLower();
        if (request.WorkLocationType.HasValue) normalized["wlt"] = request.WorkLocationType.Value.ToString();
        if (request.EmploymentType.HasValue) normalized["et"] = request.EmploymentType.Value.ToString();
        if (request.SalaryMin.HasValue) normalized["smin"] = request.SalaryMin.Value.ToString();
        if (request.SalaryMax.HasValue) normalized["smax"] = request.SalaryMax.Value.ToString();
        if (request.CompanyId.HasValue) normalized["cid"] = request.CompanyId.Value.ToString();
        if (request.JobSourceId.HasValue) normalized["sid"] = request.JobSourceId.Value.ToString();
        if (request.ExperienceYearsMax.HasValue) normalized["exp"] = request.ExperienceYearsMax.Value.ToString();
        if (!string.IsNullOrWhiteSpace(request.Tags)) normalized["tags"] = request.Tags.ToLower();
        if (request.PostedAfter.HasValue) normalized["after"] = request.PostedAfter.Value.ToString("O");
        if (!string.IsNullOrWhiteSpace(request.SortBy)) normalized["sort"] = request.SortBy.ToLower();
        if (request.SortDescending.HasValue) normalized["desc"] = request.SortDescending.Value.ToString();
        if (request.Page.HasValue) normalized["p"] = request.Page.Value.ToString();
        if (request.PageSize.HasValue) normalized["ps"] = request.PageSize.Value.ToString();

        var json = JsonSerializer.Serialize(normalized);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLower();
        return $"search:{hash}";
    }

    public static string Detail(Guid jobId)
        => $"detail:{jobId}";

    public static string Companies(string? search, int limit)
        => $"companies:{search?.ToLower() ?? ""}:{limit}";

    public static string Company(Guid companyId)
        => $"company:{companyId}";

    public static readonly string[] InvalidationPrefixes =
        ["search:", "tags:", "companies:", "company:", "stats:"];
}
