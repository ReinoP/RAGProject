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

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            return json.GetProperty("response").GetString();
        }

        public async Task<string> ParseIntentAndTaskWithGemmaAsync(string userInput)
        {
            var ret = new GemmaResult() { Intent = "RAG", Task = "" };
            var retNew = "";
            var prompt = $@"
            You are an assistant for a RAG system called CloudTasker. Your job is to understand the user's input and determine if it is related to CloudTasker. 
            If it is, identify the user's intent and the task they want to accomplish.
            If the input appears to be unrelated to CloudTasker, 
            return a string that informs user that the question is outside of RAG domain and cannot be answered.
            Otherwise return a string containing the question or task that user wants to accomplish.
            Do NOT include Python syntax, comments, explanations, or Markdown.
            Return result ONLY in valid string format.
            Do not include code fences, markdown, backtics, or quotes.
            Input: {userInput}
            ";
            try
            {
                retNew = await GenerateResponseAsync("gemma3:4b", prompt);
                //int start = resp.IndexOf('{');
                //int end = resp.LastIndexOf('}');
                //string cleanedResp = resp.Substring(start, end - start + 1);

                //ret = JsonSerializer.Deserialize<GemmaResult>(cleanedResp);
             }
            catch (Exception ex)
            {
                ;//TODO log
            }
            return retNew;// ret ?? new GemmaResult() { Intent = "RAG", Task = "Operation failed, inform user." };

        }

        public async Task<string> RunTaskWithLlamaAsync(string preprocessedInput)
        {
            var prompt = $@"
            User pre-processed input:
            {preprocessedInput}

            Keep the answer short and intelligent.
            ";

            return await GenerateResponseAsync("llama3", preprocessedInput);
        }
 
        //public async Task<string> HandleAsync(string input)
        //{
        //    var result = string.Empty;
        //    var preprocessedInput = await ParseIntentAndTaskWithGemmaAsync(input);
        //    result = await RunTaskWithLlamaAsync(preprocessedInput);


        //    if (result == null)
        //    {
        //        result = "No result generated.";
        //    }

        //    return result;
        //}      
    }
}
