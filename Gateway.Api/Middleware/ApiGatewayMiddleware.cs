using System.Diagnostics;
using System.Text.Json;
using Gateway.Core.Entities;
using Gateway.Infrastructure.Persistence;
using Gateway.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Middleware
{
    public class ApiGatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitService _rateLimit;
        private readonly ITierCache _tierCache;
        private readonly IServiceProvider _sp;

        public ApiGatewayMiddleware(RequestDelegate next, IRateLimitService rateLimit, ITierCache tierCache, IServiceProvider sp)
        {
            _next = next;
            _rateLimit = rateLimit;
            _tierCache = tierCache;
            _sp = sp;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Expect ApiKeyAuthMiddleware to have populated items
            if (!context.Items.TryGetValue("ApiKey", out var apiKeyObj) || apiKeyObj is not Gateway.Core.Entities.ApiKey apiKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing authentication");
                return;
            }

            var customer = apiKey.Customer!;
            var tier = customer.Tier ?? await _tierCache.GetTierAsync(customer.TierId);

            // Check rate-limit
            var result = await _rateLimit.CheckRequestAsync(customer.Id, tier);
            if (!result.Allowed)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (result.RetryAfter.HasValue)
                    context.Response.Headers["Retry-After"] = result.RetryAfter.Value.ToString();

                var body = new { code = "RATE_LIMIT_EXCEEDED", message = result.Reason };
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(body));
                return;
            }

            var sw = Stopwatch.StartNew();
            // allow pipeline to proceed
            await _next(context);
            sw.Stop();

            // Log successful responses asynchronously
            try
            {
                var status = context.Response.StatusCode;
                var log = new UsageLog
                {
                    CustomerId = customer.Id,
                    UserId = null, // optional: set if you have user info in header
                    Endpoint = context.Request.Path + context.Request.QueryString,
                    Timestamp = DateTime.UtcNow,
                    HttpMethod = context.Request.Method,
                    ResponseStatus = status,
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };

                // Fire-and-forget, but use IServiceScope to get DbContext
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApiMonetizationDbContext>();
                        db.UsageLogs.Add(log);
                        await db.SaveChangesAsync();
                    }
                    catch
                    {
                        // swallow to avoid crashing worker thread
                    }
                });
            }
            catch
            {
                // ignore logging failures
            }
        }
    }

    public static class ApiGatewayMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiGateway(this IApplicationBuilder builder)
            => builder.UseMiddleware<ApiGatewayMiddleware>();
    }
}
