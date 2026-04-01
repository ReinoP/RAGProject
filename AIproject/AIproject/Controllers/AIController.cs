using AIproject.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIproject.Controllers
{
    [Route("api/ai")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly RAGService _ragService;

        public AIController(RAGService ragService)
        {
            _ragService = ragService;
        }

        public class UserRequest
        {
            public string Input { get; set; }
            public int ID { get; set; }
        }

        [HttpPost("AskAI")]
        public async Task<IActionResult> AskAI([FromBody] UserRequest request)
        {
            if (String.IsNullOrEmpty(request.Input))
            {
                return BadRequest("You did not give any input.");
            }
            var answer = await _ragService.HandleAsync(request.Input);
            return Ok(new { id = request.ID + 1, type = "ai", content = answer, timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() });
        }
    }
}