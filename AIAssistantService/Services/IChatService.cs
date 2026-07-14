using AIAssistantService.DTOs;

namespace AIAssistantService.Services
{
    public interface IChatService
    {
        Task<ChatResponseDto> ChatAsync(ChatRequestDto request);
        IAsyncEnumerable<string> ChatStreamAsync(ChatRequestDto request);
    }
}