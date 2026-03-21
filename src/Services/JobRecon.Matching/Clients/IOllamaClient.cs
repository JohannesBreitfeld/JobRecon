namespace JobRecon.Matching.Clients;

public interface IOllamaClient
{
    Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
