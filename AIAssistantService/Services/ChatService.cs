using AIAssistantService.Data;
using AIAssistantService.DTOs;
using AIAssistantService.Models;
using AIAssistantService.Prompts;
using Microsoft.EntityFrameworkCore;

namespace AIAssistantService.Services
{
    public class ChatService : IChatService
    {
        private readonly IGeminiService _gemini;
        private readonly IDataContextService _dataContext;
        private readonly AIDbContext _db;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IGeminiService gemini,
            IDataContextService dataContext,
            AIDbContext db,
            ILogger<ChatService> logger)
        {
            _gemini = gemini;
            _dataContext = dataContext;
            _db = db;
            _logger = logger;
        }

        public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request)
        {
            // Step 1 — Build data context from real DB data
            var (patientContext, appointmentContext) =
                await BuildContextAsync(request.PatientId);

            // Step 2 — Build system prompt with real data
            var systemPrompt = SystemPrompt.Build(patientContext, appointmentContext);

            // Step 3 — Load conversation history for this session
            var history = await GetHistoryAsync(request.SessionId);

            // Step 4 — Call Gemini API
            var reply = await _gemini.ChatAsync(
                request.Message, systemPrompt, history);

            // Step 5 — Save messages to history
            await SaveMessagesAsync(request.SessionId, request.Message, reply);

            _logger.LogInformation(
                "Chat completed for session {SessionId}", request.SessionId);

            return new ChatResponseDto
            {
                Reply = reply,
                SessionId = request.SessionId
            };
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(ChatRequestDto request)
        {
            var (patientContext, appointmentContext) =
                await BuildContextAsync(request.PatientId);

            var systemPrompt = SystemPrompt.Build(patientContext, appointmentContext);
            var history = await GetHistoryAsync(request.SessionId);

            var fullReply = new System.Text.StringBuilder();

            await foreach (var chunk in _gemini.ChatStreamAsync(
                request.Message, systemPrompt, history))
            {
                fullReply.Append(chunk);
                yield return chunk;
            }

            // Save complete conversation after streaming finishes
            await SaveMessagesAsync(
                request.SessionId,
                request.Message,
                fullReply.ToString());
        }

        // ── Private helpers ─────────────────────────────────────

        private async Task<(string patient, string appointment)>
            BuildContextAsync(int? patientId)
        {
            if (!patientId.HasValue)
                return (string.Empty, string.Empty);

            var patientContext =
                await _dataContext.GetPatientContextAsync(patientId.Value);
            var appointmentContext =
                await _dataContext.GetAppointmentContextAsync(patientId.Value);

            return (patientContext, appointmentContext);
        }

        private async Task<List<(string Role, string Content)>>
            GetHistoryAsync(string sessionId)
        {
            // Load last 10 messages for context window management
            return await _db.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .OrderBy(m => m.CreatedAt)
                .Select(m => ValueTuple.Create(m.Role, m.Content))
                .ToListAsync();
        }

        private async Task SaveMessagesAsync(
            string sessionId, string userMessage, string assistantReply)
        {
            _db.ChatMessages.AddRange(
                new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "user",
                    Content = userMessage
                },
                new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = assistantReply
                }
            );
            await _db.SaveChangesAsync();
        }
    }
}