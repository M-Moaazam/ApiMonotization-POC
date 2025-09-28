using Gateway.Api.Middleware;
using Gateway.Core.Entities;
using Gateway.Infrastructure.Persistence;
using Gateway.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// 1. Configuration & Services
// -------------------------

// Add DbContext
builder.Services.AddDbContext<ApiMonetizationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add in-memory cache (local)
builder.Services.AddMemoryCache();

// Add Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis__Connection"];
    options.InstanceName = "GatewayCache_";
});

// Add custom services
builder.Services.AddScoped<MemoryRateLimitService>();
builder.Services.AddScoped<RedisRateLimitService>();
builder.Services.AddScoped<IRateLimitService, HybridRateLimitService>();
builder.Services.AddScoped<ITierCache, TierCache>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gateway API",
        Version = "v1",
        Description = "API Gateway with Rate Limiting and API Key Auth"
    });
});

// -------------------------
// 2. Build App
// -------------------------
var app = builder.Build();

// -------------------------
// 3. Middleware Pipeline
// -------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

app.UseHttpsRedirection();

// 🔑 1. API Key Auth Middleware (checks header & populates HttpContext.Items)
app.UseApiKeyAuth();

// 🔀 2. API Gateway Middleware (rate limiting, usage logging)
app.UseApiGateway();

app.UseAuthorization();

app.MapControllers();

app.Run();
