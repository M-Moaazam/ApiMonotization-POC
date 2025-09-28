using Gateway.Infrastructure.Services;
using Gateway.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

public class MemoryRateLimitServiceTests
{
    private readonly MemoryRateLimitService _service;
    private readonly IMemoryCache _cache;

    public MemoryRateLimitServiceTests()
    {
        _cache = new MemoryCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
        _service = new MemoryRateLimitService(_cache);
    }

    [Fact]
    public async Task Allows_Upto_RateLimit_Per_Second()
    {
        var tier = new Tier { RateLimitPerSecond = 3, MonthlyQuota = 1000 };
        int customerId = 1;
        // call 3 times -> allowed
        for (int i = 0; i < 3; i++)
        {
            var r = await _service.CheckRequestAsync(customerId, tier);
            Assert.True(r.Allowed);
        }
        // 4th should be blocked
        var res = await _service.CheckRequestAsync(customerId, tier);
        Assert.False(res.Allowed);
        Assert.Equal("Rate limit exceeded", res.Reason);
    }

    [Fact]
    public async Task Blocks_When_Monthly_Quota_Exceeded()
    {
        var tier = new Tier { RateLimitPerSecond = 1000, MonthlyQuota = 2 };
        int customerId = 2;
        // consume monthly quota
        await _service.CheckRequestAsync(customerId, tier);
        await _service.CheckRequestAsync(customerId, tier);
        var res = await _service.CheckRequestAsync(customerId, tier);
        Assert.False(res.Allowed);
        Assert.Equal("Monthly quota exceeded", res.Reason);
    }
}
