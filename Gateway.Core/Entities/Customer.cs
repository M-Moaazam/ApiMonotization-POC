namespace Gateway.Core.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int TierId { get; set; }
    public Tier Tier { get; set; } = null!;

    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
    public ICollection<MonthlySummary> MonthlySummaries { get; set; } = new List<MonthlySummary>();
}