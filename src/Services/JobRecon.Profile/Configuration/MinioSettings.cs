using System.ComponentModel.DataAnnotations;

namespace JobRecon.Profile.Configuration;

public sealed class MinioSettings
{
    public const string SectionName = "Minio";

    [Required]
    public string Endpoint { get; set; } = null!;

    [Required]
    public string AccessKey { get; set; } = null!;

    [Required]
    public string SecretKey { get; set; } = null!;

    [Required]
    public string BucketName { get; set; } = "jobrecon-cvs";

    public bool UseSSL { get; set; } = false;
}
