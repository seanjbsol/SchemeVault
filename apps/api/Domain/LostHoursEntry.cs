namespace SchemeVault.Api.Domain;

public class LostHoursEntry : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid? AccidentId { get; set; }
    public Accident? Accident { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public decimal Hours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
