namespace JobRecon.Profile.Domain;

public sealed class PreferredLocation
{
    public Guid Id { get; set; }
    public Guid JobPreferenceId { get; set; }
    public int LocalityId { get; set; }
    public required string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? MaxDistanceKm { get; set; }

    public JobPreference JobPreference { get; set; } = null!;
}
