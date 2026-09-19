namespace SchemeVault.Api.Domain;

public class QuestionnaireResponse : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string SchemeCode { get; set; } = string.Empty;
    public QuestionnaireStatus Status { get; set; }
    public string AnswersJson { get; set; } = "{}";
    public string? GeneratedMarkdown { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
