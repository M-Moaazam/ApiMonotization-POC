namespace Gateway.Core.Entities;

public class MonthlySummary
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalRequests { get; set; }

    public decimal AmountDue { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
