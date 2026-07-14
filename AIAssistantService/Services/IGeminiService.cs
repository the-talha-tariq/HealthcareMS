namespace AIAssistantService.Services
{
    public interface IGeminiService
    {
        Task<string> ChatAsync(
            string userMessage,
            string systemContext,
            List<(string Role, string Content)> history);

        IAsyncEnumerable<string> ChatStreamAsync(
            string userMessage,
            string systemContext,
            List<(string Role, string Content)> history,
            CancellationToken cancellationToken = default);
    }
}
