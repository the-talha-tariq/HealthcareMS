using AIAssistantService.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AIAssistantService.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> ChatAsync(
            string userMessage,
            string systemContext,
            List<(string Role, string Content)> history)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"] ?? "gemini-3.5-flash";
            var fallbackModel = _configuration["Gemini:FallbackModel"];

            // Build conversation history for Gemini
            var contents = new List<GeminiContent>();

            // Add system context as first user message
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = systemContext }
                }
            });

            // Add model acknowledgement
            contents.Add(new GeminiContent
            {
                Role = "model",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart
                    {
                        Text = "Understood. I am your HealthcareMS assistant. " +
                               "How can I help you today?"
                    }
                }
            });

            // Add conversation history
            foreach (var (role, content) in history)
            {
                contents.Add(new GeminiContent
                {
                    Role = role == "assistant" ? "model" : "user",
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = content }
                    }
                });
            }

            // Add current user message
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = userMessage }
                }
            });

            var request = new GeminiRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = 1024,
                    Temperature = 0.7f
                }
            };

            var result = await SendWithFallbackAsync(
                requestedModel => new HttpRequestMessage(
                    HttpMethod.Post,
                    BuildGeminiUrl(requestedModel, "generateContent", apiKey))
                {
                    Content = JsonContent.Create(request)
                },
                model,
                fallbackModel,
                HttpCompletionOption.ResponseContentRead,
                CancellationToken.None);

            using var response = result.Response;
            await EnsureGeminiSuccessAsync(response, result.Model);

            var geminiResponse = await response.Content
                .ReadFromJsonAsync<GeminiResponse>();

            return geminiResponse?.Candidates?.FirstOrDefault()
                ?.Content?.Parts?.FirstOrDefault()?.Text
                ?? "I apologize, I could not generate a response.";
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(
            string userMessage,
            string systemContext,
            List<(string Role, string Content)> history,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"] ?? "gemini-3.5-flash";
            var fallbackModel = _configuration["Gemini:FallbackModel"];

            var contents = new List<GeminiContent>();

            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = systemContext }
                }
            });

            contents.Add(new GeminiContent
            {
                Role = "model",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart
                    {
                        Text = "Understood. I am your HealthcareMS assistant."
                    }
                }
            });

            foreach (var (role, content) in history)
            {
                contents.Add(new GeminiContent
                {
                    Role = role == "assistant" ? "model" : "user",
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = content }
                    }
                });
            }

            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = userMessage }
                }
            });

            var request = new GeminiRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = 1024,
                    Temperature = 0.7f
                }
            };

            var requestJson = JsonSerializer.Serialize(request,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            var result = await SendWithFallbackAsync(
                requestedModel => new HttpRequestMessage(
                    HttpMethod.Post,
                    BuildGeminiUrl(
                        requestedModel,
                        "streamGenerateContent?alt=sse",
                        apiKey))
                {
                    Content = new StringContent(
                        requestJson, Encoding.UTF8, "application/json")
                },
                model,
                fallbackModel,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            using var response = result.Response;
            await EnsureGeminiSuccessAsync(response, result.Model);

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                // SSE format: lines start with "data: "
                if (!line.StartsWith("data: ")) continue;

                var jsonData = line["data: ".Length..];
                if (jsonData == "[DONE]") break;

                GeminiResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<GeminiResponse>(
                        jsonData,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch
                {
                    continue;
                }

                var text = chunk?.Candidates?.FirstOrDefault()
                    ?.Content?.Parts?.FirstOrDefault()?.Text;

                if (!string.IsNullOrEmpty(text))
                    yield return text;
            }
        }

        private async Task EnsureGeminiSuccessAsync(
            HttpResponseMessage response,
            string model)
        {
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogError(
                "Gemini API request failed for model {Model}. " +
                "Status: {StatusCode}. Response: {ResponseBody}",
                model,
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Gemini API request failed for model '{model}' " +
                $"with status {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                responseBody,
                inner: null,
                response.StatusCode);
        }

        private async Task<(HttpResponseMessage Response, string Model)>
            SendWithFallbackAsync(
                Func<string, HttpRequestMessage> requestFactory,
                string primaryModel,
                string? fallbackModel,
                HttpCompletionOption completionOption,
                CancellationToken cancellationToken)
        {
            var response = await SendWithRetryAsync(
                () => requestFactory(primaryModel),
                completionOption,
                cancellationToken);

            if (!IsTransient(response.StatusCode) ||
                string.IsNullOrWhiteSpace(fallbackModel) ||
                string.Equals(
                    primaryModel,
                    fallbackModel,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (response, primaryModel);
            }

            _logger.LogWarning(
                "Gemini model {PrimaryModel} remained unavailable with status " +
                "{StatusCode}. Trying fallback model {FallbackModel}.",
                primaryModel,
                (int)response.StatusCode,
                fallbackModel);

            response.Dispose();

            var fallbackResponse = await SendWithRetryAsync(
                () => requestFactory(fallbackModel),
                completionOption,
                cancellationToken);

            return (fallbackResponse, fallbackModel);
        }

        private static string BuildGeminiUrl(
            string model,
            string operation,
            string? apiKey)
        {
            var keySeparator = operation.Contains('?') ? "&" : "?";

            return "https://generativelanguage.googleapis.com/v1beta/models/" +
                   $"{model}:{operation}{keySeparator}key=" +
                   Uri.EscapeDataString(apiKey ?? string.Empty);
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(
            Func<HttpRequestMessage> requestFactory,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 4;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var request = requestFactory();

                try
                {
                    var response = await _httpClient.SendAsync(
                        request,
                        completionOption,
                        cancellationToken);

                    if (!IsTransient(response.StatusCode) || attempt == maxAttempts)
                        return response;

                    var delay = GetRetryDelay(response, attempt);

                    _logger.LogWarning(
                        "Gemini returned {StatusCode}. Retrying attempt " +
                        "{NextAttempt}/{MaxAttempts} after {DelayMs} ms.",
                        (int)response.StatusCode,
                        attempt + 1,
                        maxAttempts,
                        delay.TotalMilliseconds);

                    response.Dispose();
                    await Task.Delay(delay, cancellationToken);
                }
                catch (HttpRequestException ex) when (attempt < maxAttempts)
                {
                    var delay = GetExponentialDelay(attempt);

                    _logger.LogWarning(
                        ex,
                        "Gemini network request failed. Retrying attempt " +
                        "{NextAttempt}/{MaxAttempts} after {DelayMs} ms.",
                        attempt + 1,
                        maxAttempts,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new InvalidOperationException("Gemini retry loop ended unexpectedly.");
        }

        private static bool IsTransient(HttpStatusCode statusCode) =>
            statusCode == HttpStatusCode.RequestTimeout ||
            statusCode == HttpStatusCode.TooManyRequests ||
            (int)statusCode >= 500;

        private static TimeSpan GetRetryDelay(
            HttpResponseMessage response,
            int attempt)
        {
            var retryAfter = response.Headers.RetryAfter;

            if (retryAfter?.Delta is { } delta)
                return delta;

            if (retryAfter?.Date is { } date)
            {
                var serverDelay = date - DateTimeOffset.UtcNow;
                if (serverDelay > TimeSpan.Zero)
                    return serverDelay;
            }

            return GetExponentialDelay(attempt);
        }

        private static TimeSpan GetExponentialDelay(int attempt)
        {
            var exponentialSeconds = Math.Pow(2, attempt - 1);
            var jitterMilliseconds = Random.Shared.Next(100, 750);

            return TimeSpan.FromSeconds(exponentialSeconds) +
                   TimeSpan.FromMilliseconds(jitterMilliseconds);
        }
    }
}
