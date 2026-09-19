namespace SchemeVault.Api.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<EvidenceItem> EvidenceItems { get; set; } = new List<EvidenceItem>();
    public ICollection<Accreditation> Accreditations { get; set; } = new List<Accreditation>();
    public ICollection<GapChecklistItem> GapItems { get; set; } = new List<GapChecklistItem>();
}
