namespace SchemeVault.Api.Domain;

/// <summary>Global gap-checklist template mapped to a scheme. Instantiated per tenant as <see cref="GapChecklistItem"/>.</summary>
public class GapChecklistTemplate
{
    public Guid Id { get; set; }
    public Guid SchemeId { get; set; }
    public Scheme Scheme { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EvidenceHint { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
