namespace AIAssistantService.DTOs
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int? PatientId { get; set; } // optional context
    }

    public class ChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Gemini API request/response models
    public class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    public class GeminiContent
    {
        public string Role { get; set; } = string.Empty;
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GeminiGenerationConfig
    {
        public int MaxOutputTokens { get; set; } = 1024;
        public float Temperature { get; set; } = 0.7f;
    }

    public class GeminiResponse
    {
        public List<GeminiCandidate> Candidates { get; set; } = new();
    }

    public class GeminiCandidate
    {
        public GeminiContent Content { get; set; } = new();
    }
}