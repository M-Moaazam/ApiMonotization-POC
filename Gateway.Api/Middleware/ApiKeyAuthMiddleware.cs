using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Gateway.Api.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ApiMonetizationDbContext db)
        {
            if (!context.Request.Headers.TryGetValue("x-api-key", out var apiKeyHeader))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing API key");
                return;
            }

            var key = apiKeyHeader.ToString();
            var apiKey = await db.ApiKeys
                .Include(k => k.Customer)
                .ThenInclude(c => c.Tier)
                .FirstOrDefaultAsync(k => k.Key == key && k.IsActive);

            if (apiKey == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }

            // attach to HttpContext
            context.Items["ApiKey"] = apiKey;
            context.Items["Customer"] = apiKey.Customer; // convenience
            await _next(context);
        }
    }

    // Extension to add middleware
    public static class ApiKeyAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
            => builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
