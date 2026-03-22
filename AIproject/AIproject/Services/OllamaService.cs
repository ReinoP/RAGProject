using System.Text.Json;

namespace AIproject.Services
{
    public class OllamaService
    {
        private readonly HttpClient _http;

        public OllamaService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GenerateResponse(string model, string prompt)
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

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            return json.GetProperty("response").GetString();
        }

        public class GeminiResult
        {
            public string Intent { get; set; }
            public string Task { get; set; }
        }
        public async Task<GeminiResult> ParseIntentWithGemma(string userInput)
        {
            var prompt = $@"
            Extract intent behind the input, and return a short task based on the intent.
            Task is the instructions given to LLAMA, in a string format.
             For example, if the input is a greeting, then the task returned is 'return a greeting'.
            Or if the input is 'what is 1+1' then the task is 'calculate 1+1 and return the result'.
            Do NOT include Python syntax, comments, explanations, or Markdown, or line breaks.
            Input: {userInput}
             
            ";

            var resp = await GenerateResponse("gemma3:4b", prompt);
            var ret = new GeminiResult() { Intent = "general", Task = "" };
            if (resp != null) {
                ret.Intent = resp;
            }
            return ret;
        }
        public async Task<string> RunTaskWithLlama(string intentJson, string userInput)
        {
            var prompt = $@"
            User intent:
            {intentJson}

            User request:
            {userInput}

            Perform the task intelligently.
            ";

            return await GenerateResponse("llama3", prompt);
        }
        public async Task<string> HandleRequest(string input)
        {
            //TODO caching input: intent
            var parsedData = await ParseIntentWithGemma(input);

            //if (!intent.Contains("intent"))
            //{
            //    intent = "{ \"intent\": \"general\" }";
            //}

            var result = await RunTaskWithLlama(parsedData.Intent, input);

            if(result == null)
            {
                result = "No result generated.";
            }

            return result;
        }
    }
}
//```json
//{
//    "intent": "greeting",
//    "task": ""
//}
//```
