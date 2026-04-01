using AIproject.Data;
using AIproject.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIproject.Services
{
    public class RAGService
    {
        private readonly AppDbContext _dbContext;
        private readonly EmbeddingService _embeddingService;
        private readonly LLMService _lLMService;

        public RAGService(AppDbContext dbContext, EmbeddingService embeddingService, LLMService llmService)
        {
            _dbContext = dbContext;
            _embeddingService = embeddingService;
            _lLMService = llmService;
        }
        public async Task<List<string>> ChunkText(string text, int chunkSize = 500)
        {
            //some basic sanitization
            text = Regex.Replace(text, @"<script.*?>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<style.*?>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = text.Replace('\t', ' ');
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
            var chunks = await ChunkText(content);
            int chunkIndex = 0;
            //Could be optimized by doing embeddings in batches, but for simplicity we do it one by one here.
            foreach (var chunk in chunks)
            {
                try
                {
                    var embedding = await _embeddingService.GetEmbedding(chunk);
                    var embeddingJson = JsonSerializer.Serialize(embedding);

                    // create model
                    var docChunk = new DocumentChunk
                    {
                        Content = chunk,
                        Embedding = embeddingJson,
                        SourceFile = fileName,
                        ChunkIndex = chunkIndex
                    };
                    embeddings.Add(embeddingJson);
                    chunkIndex++;
                } 
                catch {
                    //TODO log
                    continue;
                }
            }

            if (chunks.Any())
            {
                //TODO, check if filename already has uploaded stuff, then maybe confirm from user if they want to re-upload the data?
                _dbContext.DocumentChunks.AddRange(chunks.Select((c, idx) => new DocumentChunk
                {
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
            try { 
                //TODO do not load them always. Set up a cache, and update it on upload and/or periodically.
                var chunks = await _dbContext.DocumentChunks.ToListAsync<DocumentChunk>();

                var scoredChunks = chunks.Select(c =>
                {
                    var chunkEmbedding = JsonSerializer.Deserialize<float[]>(c.Embedding);
                    if (chunkEmbedding == null) return new { Chunk = c, Score = -1.0 };

                    var score = CosineSimilarity(queryEmbedding, chunkEmbedding);
                    return new { Chunk = c, Score = score };
                });

                topChunks = scoredChunks
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => x.Chunk)
                    .ToList();

            }
            catch
            {
                //TODO log
                ;
            }
            return topChunks;
        }
        public async Task<string> HandleAsync(string input)
        {
            var topChunks = await QueryAsync(input);
            var prompt = BuildPrompt(topChunks, input);
           return await _lLMService.RunTaskWithLlamaAsync(prompt);
        }

        public async Task EmptyDocumentChunks()
        {
            _dbContext.DocumentChunks.RemoveRange(_dbContext.DocumentChunks);
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
