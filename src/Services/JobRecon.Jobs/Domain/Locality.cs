namespace JobRecon.Jobs.Domain;

public sealed class Locality
{
    public int GeoNameId { get; set; }
    public required string Name { get; set; }
    public required string AsciiName { get; set; }
    public string? AlternateNames { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public required string FeatureCode { get; set; }
    public string? Admin2Code { get; set; }
    public int Population { get; set; }
}
