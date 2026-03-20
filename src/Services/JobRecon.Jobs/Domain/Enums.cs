namespace JobRecon.Jobs.Domain;

public enum JobSourceType
{
    Manual,
    Arbetsformedlingen,
    LinkedIn,
    Indeed,
    Glassdoor,
    CustomRss,
    ApiIntegration
}

public enum EmploymentType
{
    FullTime,
    PartTime,
    Contract,
    Freelance,
    Internship,
    Temporary
}

public enum WorkLocationType
{
    OnSite,
    Remote,
    Hybrid
}

public enum JobStatus
{
    Active,
    Expired,
    Filled,
    Removed
}

public enum SavedJobStatus
{
    Saved,
    Applied,
    Interviewing,
    Rejected,
    Offered,
    Accepted,
    Withdrawn
}
