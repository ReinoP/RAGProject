using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace AIproject.Services
{
    [Authorize]
    public class EmbeddingService
    {
        private readonly HttpClient _http;
        public EmbeddingService(HttpClient http)
        {
            _http = http;
        }

        public async Task<float[]> GetEmbedding(string text)
        {
            float[] embedding = Array.Empty<float>();
            try {
                //Can return one or more embeddings, changing the resulting JSON structure
                var url = "http://localhost:11434/api/embed";//TODO move to config
                var body = new
                {
                    model = "embeddinggemma",
                    input = text
                };
                var response = await _http.PostAsJsonAsync(url, body);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("embedding", out var single))
                {
                    embedding = single.EnumerateArray()
                                        .Select(x => x.GetSingle())
                                        .ToArray();
                }
                else if (doc.RootElement.TryGetProperty("embeddings", out var multiple))
                {
                    embedding = multiple.EnumerateArray()
                        .First()
                        .EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray();
                }

            }
            catch
            {
                //Here there could be some logging
            }
            return embedding;
        }
    }
}
