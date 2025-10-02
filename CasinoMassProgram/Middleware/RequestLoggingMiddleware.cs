using System.Diagnostics;

namespace CasinoMassProgram.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var correlationId = context.TraceIdentifier;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var status = context.Response?.StatusCode;
                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                    method, path, status, sw.ElapsedMilliseconds);
            }
        }
    }
}