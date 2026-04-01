using AIproject.Models;
using System.Text.Json;

namespace AIproject.Services
{
    public class LLMService
    {
        private readonly HttpClient _http;
        public LLMService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GenerateResponseAsync(string model, string prompt)
        {
            var request = new
            {
                model = model,
                prompt = prompt,
                stream = false
            };

            var response = await _http.PostAsJsonAsync(
                "http://localhost:11434/api/generate",
                request
            );

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (!json.TryGetProperty("response", out var responseProp))
            {
                return string.Empty;
            }

            return responseProp.GetString() ?? string.Empty;
        }

        public async Task<string> RunTaskWithLlamaAsync(string preprocessedPrompt)
        {
            return await GenerateResponseAsync("llama3", preprocessedPrompt);
        }   
    }
}
