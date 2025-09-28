namespace Gateway.Core.Entities;

public class Tier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int MonthlyQuota { get; set; }
    public int RateLimitPerSecond { get; set; }
    public decimal PricePerMonth { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
