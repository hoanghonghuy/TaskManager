using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Services;
using System.Threading.Tasks;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly AIService _aiService;

        public AIController(AIService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("parse-task")]
        
        
        public async Task<IActionResult> ParseTask([FromBody] ParseTaskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest("Input text cannot be empty.");
            }

            var parsedInfo = await _aiService.ParseTaskFromTextAsync(request.Text);

            if (parsedInfo == null)
            {
                return StatusCode(500, "Failed to parse task information from AI service.");
            }

            return Ok(parsedInfo);
        }
    }

    public class ParseTaskRequest
    {
        public string? Text { get; set; }
    }
}