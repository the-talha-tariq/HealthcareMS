using AIAssistantService.DTOs;
using AIAssistantService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace AIAssistantService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Standard (non-streaming) endpoint
        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest(new { message = "Message cannot be empty." });

            if (string.IsNullOrEmpty(request.SessionId))
                request.SessionId = Guid.NewGuid().ToString();

            try
            {
                var response = await _chatService.ChatAsync(request);
                return Ok(response);
            }
            catch (HttpRequestException ex) when (
                ex.StatusCode == HttpStatusCode.RequestTimeout ||
                ex.StatusCode == HttpStatusCode.TooManyRequests ||
                (int?)ex.StatusCode >= 500)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message = "The AI provider is temporarily unavailable. " +
                                  "Please try again shortly."
                    });
            }
        }

        // Streaming endpoint — sends response token by token
        [HttpPost("stream")]
        public async Task ChatStream([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrEmpty(request.SessionId))
                request.SessionId = Guid.NewGuid().ToString();

            // Set response headers for Server-Sent Events (SSE)
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            await foreach (var chunk in _chatService.ChatStreamAsync(request))
            {
                // SSE format: "data: {content}\n\n"
                var data = $"data: {chunk}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await Response.Body.WriteAsync(bytes);
                await Response.Body.FlushAsync();
            }

            // Signal end of stream
            var done = "data: [DONE]\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(done));
        }

        // Get chat history for a session
        [HttpGet("history/{sessionId}")]
        public async Task<IActionResult> GetHistory(string sessionId)
        {
            // Will implement if needed
            return Ok(new { sessionId, message = "History endpoint ready." });
        }
    }
}
