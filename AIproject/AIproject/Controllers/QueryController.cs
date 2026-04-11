using AIproject.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AIproject.Controllers
{
    [Route("api/ai")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly RAGService _ragService;

        public QueryController(RAGService ragService)
        {
            _ragService = ragService;
        }

        public class UserRequest
        {
            [Required]
            public required string Input { get; set; }
            [Required]
            public int ID { get; set; }
        }
        public class AIResponse
        {
            public required int ID { get; set; }
            public required string Type { get; set; }
            public required string Content { get; set; }
            public required long Timestamp { get; set; }
        }

        [HttpPost("AskAI")]
        public async Task<IActionResult> Query([FromBody] UserRequest request)
        {
            var answer = await _ragService.HandleAsync(request.Input);
            return Ok(new AIResponse { ID = request.ID + 1, Type = "ai", Content = answer, Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() });
        }
    }
}