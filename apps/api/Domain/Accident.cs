namespace SchemeVault.Api.Domain;

public class Accident : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateTimeOffset OccurredOn { get; set; }
    public string Location { get; set; } = string.Empty;
    public AccidentSeverity Severity { get; set; }
    public AccidentStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? InjuredPerson { get; set; }
    public string? ImmediateAction { get; set; }
    public decimal LostHours { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<LostHoursEntry> LostHoursEntries { get; set; } = new List<LostHoursEntry>();
}
