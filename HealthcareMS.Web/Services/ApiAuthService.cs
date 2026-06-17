using HealthcareMS.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Json;
using System.Security.Claims;

namespace HealthcareMS.Web.Services
{
    public class ApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiAuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", new LoginModel
            {
                Email = email,
                Password = password
            });

            if (!response.IsSuccessStatusCode)
            {
                return (false, "Invalid email or password.");
            }

            var authResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResult == null)
            {
                return (false, "Unexpected error during login.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, authResult.FullName),
                new Claim(ClaimTypes.Email, authResult.Email),
                new Claim(ClaimTypes.Role, authResult.Role),
                new Claim("access_token", authResult.Token)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = _httpContextAccessor.HttpContext!;
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = authResult.ExpiresAt
                });

            return (true, null);
        }

        public async Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext!;
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public string? GetToken()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst("access_token")?.Value;
        }
    }
}
