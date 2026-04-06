namespace EverythingServer.Middleware;

public class UserAgentLoggingMiddleware(RequestDelegate next, ILogger<UserAgentLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
            logger.LogInformation("Incoming request {Method} {Path} — User-Agent: (none)",
                context.Request.Method, context.Request.Path);
        else
            logger.LogInformation("Incoming request {Method} {Path} — User-Agent: {UserAgent}",
                context.Request.Method, context.Request.Path, userAgent);

        await next(context);
    }
}
