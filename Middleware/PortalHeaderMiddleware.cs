using CampusHub.ConfigCenter.Models;
using Microsoft.Extensions.Options;

namespace CampusHub.ConfigCenter.Middleware;

/// <summary>
/// Middleware, который добавляет заголовки X-Portal-Title и X-Portal-Semester
/// в каждый HTTP-ответ, используя IOptions&lt;PortalOptions&gt;.
/// </summary>
public class PortalHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PortalOptions _options;

    public PortalHeaderMiddleware(RequestDelegate next, IOptions<PortalOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Добавляем заголовки до передачи управления следующему middleware
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Portal-Title"] = _options.Title;
            context.Response.Headers["X-Portal-Semester"] = _options.Semester;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
