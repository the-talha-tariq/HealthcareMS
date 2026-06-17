
using HealthcareMS.Web.Authentication;
using HealthcareMS.Web.Components;
using HealthcareMS.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpContextAccessor is needed to read auth cookie in services.
builder.Services.AddHttpContextAccessor();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// JWT Handler attaches token to outgoing requests.
builder.Services.AddTransient<JwtAuthHandler>();

// API Base Address points to our Gateway.
var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "http://localhost:5107/";

// Auth Service (no JWT handler needed because it is used before login).
builder.Services.AddHttpClient<ApiAuthService>(client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
});

// Patient API (with JWT handler)
builder.Services.AddHttpClient<PatientApiService>(client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
}).AddHttpMessageHandler<JwtAuthHandler>();

// Appointment API (with JWT handler)
builder.Services.AddHttpClient<AppointmentApiService>(client =>
{
    client.BaseAddress = new Uri(gatewayUrl);
}).AddHttpMessageHandler<JwtAuthHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/login-submit", async (HttpContext context, ApiAuthService authService) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var (success, error) = await authService.LoginAsync(email, password);
    if (success)
    {
        return Results.Redirect("/patients");
    }

    return Results.Redirect($"/login?error={Uri.EscapeDataString(error ?? "Login failed.")}");
});

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();
