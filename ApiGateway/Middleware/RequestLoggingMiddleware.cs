namespace ApiGateway.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "──► Incoming Request: {Method} {Path} at {Time}",
                context.Request.Method,
                context.Request.Path,
                startTime.ToString("HH:mm:ss"));

            await _next(context);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "◄── Response: {StatusCode} in {Duration}ms for {Method} {Path}",
                context.Response.StatusCode,
                Math.Round(duration, 2),
                context.Request.Method,
                context.Request.Path);
        }
    }
}