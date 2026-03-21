namespace JobRecon.Matching.Configuration;

public sealed class QdrantSettings
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int GrpcPort { get; set; } = 6334;
    public int VectorSize { get; set; } = 768; // nomic-embed-text dimension
}
