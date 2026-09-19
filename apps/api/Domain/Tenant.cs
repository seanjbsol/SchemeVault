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
    public ICollection<PhotoAttachment> Photos { get; set; } = new List<PhotoAttachment>();
    public ICollection<Accident> Accidents { get; set; } = new List<Accident>();
    public ICollection<LostHoursEntry> LostHours { get; set; } = new List<LostHoursEntry>();
    public ICollection<EquipmentItem> Equipment { get; set; } = new List<EquipmentItem>();
    public ICollection<QuestionnaireResponse> QuestionnaireResponses { get; set; } = new List<QuestionnaireResponse>();
}
