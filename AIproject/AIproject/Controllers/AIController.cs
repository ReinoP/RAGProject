using AIproject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AIproject.Controllers
{
    [Route("api/ai")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly OllamaService _ollamaService;
        public AIController(OllamaService ollamaService) {
            _ollamaService = ollamaService;
        }

        public class UserRequest
        {
            public string Input { get; set; }
        }
        [HttpPost("AskAI")]
        public async Task<IActionResult> AskAI([FromBody] UserRequest request)
        {
            if (String.IsNullOrEmpty(request.Input))
            {
                return BadRequest("You did not give any input.");
            }
            var answer = await _ollamaService.HandleRequest(request.Input);
            return Ok(new { message = answer });
        }
    }
}
