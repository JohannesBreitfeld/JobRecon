using System.Net.Http.Json;
using System.Text.Json.Serialization;
using JobRecon.Matching.Configuration;
using Microsoft.Extensions.Options;

namespace JobRecon.Matching.Clients;

public sealed class OllamaClient(
    HttpClient httpClient,
    IOptions<OllamaSettings> options,
    ILogger<OllamaClient> logger) : IOllamaClient
{
    private readonly OllamaSettings _settings = options.Value;

    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var request = new OllamaEmbedRequest(_settings.EmbeddingModel, text);
            var response = await httpClient.PostAsJsonAsync("/api/embed", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama embed request failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(ct);
            if (result?.Embeddings is null || result.Embeddings.Count == 0)
            {
                logger.LogWarning("Ollama returned empty embeddings");
                return null;
            }

            return result.Embeddings[0];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating embedding via Ollama");
            return null;
        }
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] List<float[]> Embeddings);
}
