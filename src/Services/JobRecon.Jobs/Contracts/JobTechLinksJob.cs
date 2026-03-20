using System.Text.Json.Serialization;

namespace JobRecon.Jobs.Contracts;

/// <summary>
/// Response from the daily JobTech Links tar.gz files.
/// Files contain JSON with job ads from multiple sources.
/// </summary>
public sealed class JobTechLinksResponse
{
    [JsonPropertyName("total")]
    public JobTechLinksTotal? Total { get; set; }

    [JsonPropertyName("hits")]
    public List<JobTechLinksHit>? Hits { get; set; }
}

public sealed class JobTechLinksTotal
{
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public sealed class JobTechLinksHit
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("brief")]
    public string? Brief { get; set; }

    [JsonPropertyName("description")]
    public JobTechLinksDescription? Description { get; set; }

    [JsonPropertyName("occupation")]
    public JobTechLinksLabel? Occupation { get; set; }

    [JsonPropertyName("occupation_group")]
    public JobTechLinksLabel? OccupationGroup { get; set; }

    [JsonPropertyName("occupation_field")]
    public JobTechLinksLabel? OccupationField { get; set; }

    [JsonPropertyName("employer")]
    public JobTechLinksEmployer? Employer { get; set; }

    [JsonPropertyName("workplace_addresses")]
    public List<JobTechLinksAddress>? WorkplaceAddresses { get; set; }

    [JsonPropertyName("publication_date")]
    public DateTime? PublicationDate { get; set; }

    [JsonPropertyName("last_publication_date")]
    public DateTime? LastPublicationDate { get; set; }

    [JsonPropertyName("application_deadline")]
    public DateTime? ApplicationDeadline { get; set; }

    [JsonPropertyName("source_links")]
    public List<JobTechLinksSourceLink>? SourceLinks { get; set; }

    [JsonPropertyName("removed")]
    public bool Removed { get; set; }

    [JsonPropertyName("removed_date")]
    public DateTime? RemovedDate { get; set; }

    [JsonPropertyName("employment_type")]
    public JobTechLinksLabel? EmploymentType { get; set; }

    [JsonPropertyName("working_hours_type")]
    public JobTechLinksLabel? WorkingHoursType { get; set; }

    [JsonPropertyName("duration")]
    public JobTechLinksLabel? Duration { get; set; }

    [JsonPropertyName("salary_type")]
    public JobTechLinksLabel? SalaryType { get; set; }

    [JsonPropertyName("salary_description")]
    public string? SalaryDescription { get; set; }

    [JsonPropertyName("number_of_vacancies")]
    public int? NumberOfVacancies { get; set; }

    [JsonPropertyName("driving_license_required")]
    public bool? DrivingLicenseRequired { get; set; }

    [JsonPropertyName("driving_license")]
    public List<JobTechLinksLabel>? DrivingLicense { get; set; }

    [JsonPropertyName("access_to_own_car")]
    public bool? AccessToOwnCar { get; set; }

    [JsonPropertyName("must_have")]
    public JobTechLinksRequirements? MustHave { get; set; }

    [JsonPropertyName("nice_to_have")]
    public JobTechLinksRequirements? NiceToHave { get; set; }

    [JsonPropertyName("experience_required")]
    public bool? ExperienceRequired { get; set; }

    [JsonPropertyName("access")]
    public string? Access { get; set; }

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }
}

public sealed class JobTechLinksDescription
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("text_formatted")]
    public string? TextFormatted { get; set; }
}

public sealed class JobTechLinksLabel
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("concept_id")]
    public string? ConceptId { get; set; }

    [JsonPropertyName("legacy_ams_taxonomy_id")]
    public string? LegacyAmsTaxonomyId { get; set; }
}

public sealed class JobTechLinksEmployer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("organization_number")]
    public string? OrganizationNumber { get; set; }

    [JsonPropertyName("workplace")]
    public string? Workplace { get; set; }
}

public sealed class JobTechLinksAddress
{
    [JsonPropertyName("municipality")]
    public string? Municipality { get; set; }

    [JsonPropertyName("municipality_code")]
    public string? MunicipalityCode { get; set; }

    [JsonPropertyName("municipality_concept_id")]
    public string? MunicipalityConceptId { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("region_code")]
    public string? RegionCode { get; set; }

    [JsonPropertyName("region_concept_id")]
    public string? RegionConceptId { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("country_concept_id")]
    public string? CountryConceptId { get; set; }

    [JsonPropertyName("street_address")]
    public string? StreetAddress { get; set; }

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("coordinates")]
    public List<double>? Coordinates { get; set; }
}

public sealed class JobTechLinksSourceLink
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public sealed class JobTechLinksRequirements
{
    [JsonPropertyName("skills")]
    public List<JobTechLinksLabel>? Skills { get; set; }

    [JsonPropertyName("languages")]
    public List<JobTechLinksLabel>? Languages { get; set; }

    [JsonPropertyName("work_experiences")]
    public List<JobTechLinksLabel>? WorkExperiences { get; set; }

    [JsonPropertyName("education")]
    public List<JobTechLinksLabel>? Education { get; set; }

    [JsonPropertyName("education_level")]
    public List<JobTechLinksLabel>? EducationLevel { get; set; }
}

/// <summary>
/// Configuration for JobTechLinks fetcher stored in JobSource.Configuration
/// </summary>
public sealed class JobTechLinksConfig
{
    public string? LastDownloadedDate { get; set; }
    public int MaxDaysToFetch { get; set; } = 7;
}
