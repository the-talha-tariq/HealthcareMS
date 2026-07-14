using HealthcareMS.Web.Models;
using System.Net.Http.Json;

namespace HealthcareMS.Web.Services
{
    public class AIAssistantApiService
    {
        private readonly HttpClient _httpClient;

        public AIAssistantApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(ChatResponseModel? Response, string? Error)> ChatAsync(
            string message,
            string sessionId,
            int? patientId)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "gateway/ai/chat",
                new { message, sessionId, patientId });

            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync();
                var error = $"AI Assistant request failed " +
                    $"({(int)response.StatusCode} {response.ReasonPhrase}).";

                if (!string.IsNullOrWhiteSpace(details))
                    error += $" {details}";

                return (null, error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<ChatResponseModel>();

            return result == null
                ? (null, "AI Assistant returned an empty response.")
                : (result, null);
        }
    }
}
