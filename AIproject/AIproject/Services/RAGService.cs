using AIproject.Data;
using AIproject.Interfaces;
using AIproject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIproject.Services
{
    public class RAGService
    {
        private readonly AppDbContext _dbContext;
        private readonly EmbeddingService _embeddingService;
        private readonly LLMService _lLMService;
        private readonly IMemoryCache _cache;
        private readonly IUserContext _userContext;
        private readonly string? _userId;
        public RAGService(AppDbContext dbContext, EmbeddingService embeddingService, LLMService llmService, IMemoryCache cache, IUserContext userContext)
        {
            _dbContext = dbContext;
            _embeddingService = embeddingService;
            _lLMService = llmService;
            _cache = cache;
            _userContext = userContext;

            _userId = _userContext.GetUserId();
            var name = _userContext.GetUserName();

        }
        public async Task<List<string>> ChunkText(string text, int chunkSize = 500)
        {
            //some basic sanitization
            text = Regex.Replace(text, @"<script.*?>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<style.*?>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+", " ");
            text = Regex.Replace(text, @"[#*_>`~\-]+", "");
            text = Regex.Replace(text, "<.*?>", "").Trim();

            var chunks = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }
            return chunks;
        }

        public async Task ProcessDocumentAsync(string content, string fileName)
        {
            var embeddings = new List<string>();
            //first we could check if this file has already been processed, and if so, skip it. For simplicity we skip this step here, but in production it would be crucial to avoid duplicates and save resources.
            var chunks = await ChunkText(content);
            int chunkIndex = 0;
            //Could be optimized by doing embeddings in batches, but for simplicity we do it one by one here.
            foreach (var chunk in chunks)
            {
                try
                {
                    var embedding = await _embeddingService.GetEmbedding(chunk);
                    var embeddingJson = JsonSerializer.Serialize(embedding);

                    var docChunk = new DocumentChunk
                    {
                        UserId = _userId,
                        Content = chunk,
                        Embedding = embeddingJson,
                        SourceFile = fileName,
                        ChunkIndex = chunkIndex
                    };
                    embeddings.Add(embeddingJson);
                    chunkIndex++;
                } 
                catch {
                    //Here there could be some logging
                    continue;
                }
            }

            if (chunks.Any())
            {
                _cache.Remove("chunks_"+ _userId);
                
                _dbContext.DocumentChunks.AddRange(chunks.Select((c, idx) => new DocumentChunk
                {
                    UserId = _userId,
                    Content = c,
                    Embedding = embeddings[idx],
                    SourceFile = fileName,
                    ChunkIndex = idx
                }));
                _dbContext.SaveChanges();
            }
        }

        //https://www.ibm.com/think/topics/cosine-similarity
        private double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, magA = 0, magB = 0;

            if(a.Length != b.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        public async Task<List<DocumentChunk>> QueryAsync(string userQuestion, int topK = 10)
        {
            var queryEmbedding = await _embeddingService.GetEmbedding(userQuestion);
            var topChunks = new List<DocumentChunk>();
            
            try
            { 
                var chunks =  new List<DocumentChunk>();
                if (_cache.TryGetValue("chunks_" + _userId, out List<DocumentChunk>? cachedChunks)){
                    chunks = cachedChunks;
                }
                else {
                    chunks = await _dbContext.DocumentChunks.Where(dc => dc.UserId == (_userId ?? string.Empty)).ToListAsync<DocumentChunk>();
                    var cacheOptions = new MemoryCacheEntryOptions()
                                       .SetSlidingExpiration(TimeSpan.FromMinutes(10)) 
                                       .SetAbsoluteExpiration(TimeSpan.FromMinutes(15)); 
                    _cache.Set("chunks_" + _userId, chunks, cacheOptions);
                }

                var scoredChunks = chunks?.Select(c =>
                {
                    var chunkEmbedding = JsonSerializer.Deserialize<float[]>(c.Embedding);
                    if (chunkEmbedding == null) return new { Chunk = c, Score = -1.0 };

                    var score = CosineSimilarity(queryEmbedding, chunkEmbedding);
                    return new { Chunk = c, Score = score };
                });

                topChunks = scoredChunks?
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => x.Chunk)
                    .ToList();
            }
            catch
            {
                //Here there could be some logging
            }
            return topChunks ?? new List<DocumentChunk>();
        }

        public async Task<string> HandleAsync(string input)
        {
            var topChunks = await QueryAsync(input);
            var prompt = BuildPrompt(topChunks, input);
           return await _lLMService.RunTaskWithLlamaAsync(prompt);
        }
        
        public async Task EmptyDocumentChunks()
        {
            _cache.Remove("chunks_" + _userId);
            var relatedUserChunks = _dbContext.DocumentChunks.Where(dc => dc.UserId == _userId).ToList();
            _dbContext.DocumentChunks.RemoveRange(relatedUserChunks);
            _dbContext.SaveChanges();

        }

        private string BuildPrompt(List<DocumentChunk> topChunks, string userQuestion)
        {
            if (topChunks == null || topChunks.Count == 0)
            {
                return $"No relevant information found. Inform the user there is no context uploaded.";
            }

            var context = string.Join("\n---\n", topChunks.Select(c => c.Content));

            return $@"
                You are an assistant for a RAG system. Your job is to understand the user's input and determine if it is related to the data user has uploaded. 
                If it is, identify the user's intent and the task they want to accomplish.
                Do not use previous knowledge or make assumptions.
                If the answer is not found in the given context, respond with 'Answer to this question is not found in the provided data'.
                Do not infer, speculate, guess or fill gaps.
                Do NOT include Python syntax, comments, explanations, or Markdown.
                Use tags like <p>, <ul>, <li>, <b>.
                    - Use <p> for paragraphs
                    - Use <ul><li> for lists
                    - Use <b> for important labels
                    - Keep structure simple and readable
                Return valid HTML only.

                Use ONLY the following context to answer the question:

                {context}

                Question: {userQuestion}";
        }
    }
}
