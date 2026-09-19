namespace SchemeVault.Api.Domain;

/// <summary>Global scheme catalogue (CHAS, Constructionline, …). Not tenant-owned.</summary>
public class Scheme
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSsipStyle { get; set; }
    public int SortOrder { get; set; }

    public ICollection<GapChecklistTemplate> GapTemplates { get; set; } = new List<GapChecklistTemplate>();
    public ICollection<Accreditation> Accreditations { get; set; } = new List<Accreditation>();
}
