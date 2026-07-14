using AIAssistantService.Data;
using AIAssistantService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Chat history DB — AI service's own database
builder.Services.AddDbContext<AIDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ChatDB")));

// Read-only contexts for data owned by the other services
builder.Services.AddDbContext<PatientReadDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PatientDB")));

builder.Services.AddDbContext<AppointmentReadDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AppointmentDB")));

// HttpClient for Gemini API
builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Services
builder.Services.AddScoped<IDataContextService, DataContextService>();
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

// Auto-create ChatDB and migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AIDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
