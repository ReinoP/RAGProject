using AIproject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AIproject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly RAGService _ragService;
        public ApiController(RAGService ragService )
        {
            _ragService = ragService;
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile fileModel)
        {
            if (fileModel == null || fileModel.Length == 0)
                return BadRequest("No file");
            try
            {
                using var stream = new MemoryStream();
                await fileModel.CopyToAsync(stream);

                var content = Encoding.UTF8.GetString(stream.ToArray());
                if (content.Length > 10000)
                {
                    return BadRequest("File too large. Max allowed is 10,000 characters.");
                }
                else if (content.Length == 0)
                {
                    return BadRequest("File is empty.");
                }

                await _ragService.ProcessDocumentAsync(content, fileModel.FileName);

                return Ok(new { message = "File uploaded and processed" });
            }
            catch(Exception ex)
            {
                //Here there could be some logging
                return StatusCode(500, new { success = false, error = "Something went wrong with upload." }); 
            }
        }

        [HttpDelete("DeleteRAGData")]
        public async Task<IActionResult> DeleteRAGData()
        {
            await _ragService.EmptyDocumentChunks();
          
            return Ok();
        }
    }
}
