using System.ComponentModel.DataAnnotations;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string OrganisationName { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class UpdateTenantRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public sealed class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class EvidenceWriteRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTimeOffset? ExpiresOn { get; set; }
}

public sealed class EvidenceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public string TrafficLight { get; set; } = string.Empty;
    public bool HasFile { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public int PhotoCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RenewalWriteRequest
{
    [Required]
    public Guid SchemeId { get; set; }

    [Required]
    public AccreditationStatus Status { get; set; }

    public DateTimeOffset? ExpiresOn { get; set; }
    public DateTimeOffset? LastSubmittedOn { get; set; }

    [StringLength(80)]
    public string? MembershipNumber { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed class RenewalDto
{
    public Guid Id { get; set; }
    public Guid SchemeId { get; set; }
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresOn { get; set; }
    public DateTimeOffset? LastSubmittedOn { get; set; }
    public string? MembershipNumber { get; set; }
    public string? Notes { get; set; }
    public string TrafficLight { get; set; } = string.Empty;
    public int? DaysUntilExpiry { get; set; }
}

public sealed class GapItemDto
{
    public Guid Id { get; set; }
    public Guid SchemeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? EvidenceItemId { get; set; }
    public string? EvidenceTitle { get; set; }
}

public sealed class UpdateGapItemRequest
{
    public GapItemStatus Status { get; set; }
    public Guid? EvidenceItemId { get; set; }
}

public sealed class SchemeSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool IsSsipStyle { get; set; }
    public RenewalDto? Accreditation { get; set; }
    public int MissingGaps { get; set; }
    public int CompleteGaps { get; set; }
}

public sealed class SchemeDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSsipStyle { get; set; }
    public RenewalDto? Accreditation { get; set; }
    public IReadOnlyList<GapItemDto> Gaps { get; set; } = Array.Empty<GapItemDto>();
}

public sealed class DashboardDto
{
    public string TenantName { get; set; } = string.Empty;
    public DashboardCountsDto Counts { get; set; } = new();
    public IReadOnlyList<RenewalDto> UpcomingRenewals { get; set; } = Array.Empty<RenewalDto>();
    public IReadOnlyList<RenewalDto> ExpiredRenewals { get; set; } = Array.Empty<RenewalDto>();
    public IReadOnlyList<GapItemDto> MissingEvidence { get; set; } = Array.Empty<GapItemDto>();
}

public sealed class DashboardCountsDto
{
    public int Upcoming { get; set; }
    public int Expired { get; set; }
    public int MissingEvidence { get; set; }
    public int ActiveSchemes { get; set; }
    public int EvidenceItems { get; set; }
    public int OpenAccidents { get; set; }
    public int OverdueEquipment { get; set; }
    public decimal LostHoursLast12Months { get; set; }
}

public sealed class PhotoDto
{
    public Guid Id { get; set; }
    public string OwnerKind { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string? Caption { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AccidentWriteRequest
{
    [Required]
    public DateTimeOffset OccurredOn { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public AccidentSeverity Severity { get; set; }

    public AccidentStatus Status { get; set; } = AccidentStatus.Open;

    [Required, StringLength(4000, MinimumLength = 4)]
    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? InjuredPerson { get; set; }

    [StringLength(2000)]
    public string? ImmediateAction { get; set; }

    [Range(0, 10000)]
    public decimal LostHours { get; set; }
}

public sealed class AccidentDto
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? InjuredPerson { get; set; }
    public string? ImmediateAction { get; set; }
    public decimal LostHours { get; set; }
    public int PhotoCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class LostHoursWriteRequest
{
    [Required]
    public DateTimeOffset OccurredOn { get; set; }

    [Range(0.25, 10000)]
    public decimal Hours { get; set; }

    [Required, StringLength(300, MinimumLength = 2)]
    public string Reason { get; set; } = string.Empty;

    public Guid? AccidentId { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed class LostHoursDto
{
    public Guid Id { get; set; }
    public Guid? AccidentId { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public decimal Hours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LostHoursSummaryDto
{
    public decimal TotalHours { get; set; }
    public IReadOnlyList<LostHoursDto> Entries { get; set; } = Array.Empty<LostHoursDto>();
}

public sealed class EquipmentWriteRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    [StringLength(80)]
    public string? SerialNumber { get; set; }

    public DateTimeOffset? CalibrationDueOn { get; set; }
    public DateTimeOffset? ServiceDueOn { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed class EquipmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public DateTimeOffset? CalibrationDueOn { get; set; }
    public DateTimeOffset? ServiceDueOn { get; set; }
    public string? Notes { get; set; }
    public bool IsOverdue { get; set; }
    public string TrafficLight { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class QuestionnaireQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Help { get; set; } = string.Empty;
    public string Kind { get; set; } = "text";
    public bool Required { get; set; } = true;
}

public sealed class QuestionnaireTemplateDto
{
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public Guid? LatestResponseId { get; set; }
    public DateTimeOffset? LatestGeneratedAt { get; set; }
    public IReadOnlyList<QuestionnaireQuestionDto> Questions { get; set; } = Array.Empty<QuestionnaireQuestionDto>();
}

public sealed class QuestionnaireAnswerRequest
{
    public Dictionary<string, string?> Answers { get; set; } = new();
    public bool Generate { get; set; } = true;
}

public sealed class QuestionnaireExportRequest
{
    public string[]? SchemeCodes { get; set; }
}

public sealed class QuestionnaireResponseDto
{
    public Guid Id { get; set; }
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string?> Answers { get; set; } = new();
    public string? GeneratedMarkdown { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

