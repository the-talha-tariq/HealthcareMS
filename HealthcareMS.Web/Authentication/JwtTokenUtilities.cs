using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace HealthcareMS.Web.Authentication
{
    internal static class JwtTokenUtilities
    {
        public static bool IsUnexpired(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return false;

                var payload = WebEncoders.Base64UrlDecode(parts[1]);
                using var document = JsonDocument.Parse(payload);

                if (!document.RootElement.TryGetProperty("exp", out var exp) ||
                    !exp.TryGetInt64(out var expirySeconds))
                {
                    return false;
                }

                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expirySeconds);
                return expiresAt > DateTimeOffset.UtcNow;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
