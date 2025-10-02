using System.Net;
using System.Text.Json;

public class ApiMiddleware
{
    private readonly RequestDelegate _next;

    public ApiMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bỏ qua swagger và endpoint tĩnh
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Đọc body đã được action ghi
            responseBody.Seek(0, SeekOrigin.Begin);
            var bodyText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            var contentType = context.Response.ContentType ?? string.Empty;
            var isJson = contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

            // Không phải JSON => trả nguyên vẹn (vd: file download .xlsx)
            if (!isJson)
            {
                await responseBody.CopyToAsync(originalBodyStream);
                return;
            }

            // Chuẩn hoá JSON trả về
            object? data;
            try
            {
                data = string.IsNullOrWhiteSpace(bodyText) ? null : JsonSerializer.Deserialize<object>(bodyText);
            }
            catch
            {
                data = bodyText;
            }

            var result = new
            {
                status = context.Response.StatusCode,
                data,
                success = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300
            };

            // Ghi thẳng vào stream gốc
            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json";
            // Xoá Content-Length cũ nếu có
            context.Response.Headers.ContentLength = null;

            var json = JsonSerializer.Serialize(result);
            await context.Response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            context.Response.Body = originalBodyStream;

            var errorResult = new
            {
                status = (int)HttpStatusCode.InternalServerError,
                data = ex.Message,
                success = false
            };
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            context.Response.Headers.ContentLength = null;

            var json = JsonSerializer.Serialize(errorResult);
            await context.Response.WriteAsync(json);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}