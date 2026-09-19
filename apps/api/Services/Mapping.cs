using SchemeVault.Api.Contracts;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public static class Mapping
{
    public static EvidenceDto ToDto(this EvidenceItem item, DateTimeOffset utcNow) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Category = item.Category,
        Notes = item.Notes,
        ExpiresOn = item.ExpiresOn,
        TrafficLight = TrafficLights.ForExpiry(item.ExpiresOn, utcNow).ToString(),
        HasFile = !string.IsNullOrWhiteSpace(item.StoredFileName),
        OriginalFileName = item.OriginalFileName,
        ContentType = item.ContentType,
        FileSizeBytes = item.FileSizeBytes,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };

    public static RenewalDto ToDto(this Accreditation item, DateTimeOffset utcNow)
    {
        int? days = item.ExpiresOn is null
            ? null
            : (int)Math.Floor((item.ExpiresOn.Value - utcNow).TotalDays);

        return new RenewalDto
        {
            Id = item.Id,
            SchemeId = item.SchemeId,
            SchemeCode = item.Scheme.Code,
            SchemeName = item.Scheme.Name,
            Status = TrafficLights.DerivedStatus(item.ExpiresOn, utcNow, item.Status).ToString(),
            ExpiresOn = item.ExpiresOn,
            LastSubmittedOn = item.LastSubmittedOn,
            MembershipNumber = item.MembershipNumber,
            Notes = item.Notes,
            TrafficLight = TrafficLights.ForAccreditation(item.Status, item.ExpiresOn, utcNow).ToString(),
            DaysUntilExpiry = days
        };
    }

    public static GapItemDto ToDto(this GapChecklistItem item) => new()
    {
        Id = item.Id,
        SchemeId = item.SchemeId,
        Title = item.Title,
        Description = item.Description,
        Status = item.Status.ToString(),
        EvidenceItemId = item.EvidenceItemId,
        EvidenceTitle = item.EvidenceItem?.Title
    };

    public static UserDto ToDto(this ApplicationUser user, string tenantName, MembershipRole role) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        TenantId = user.TenantId,
        TenantName = tenantName,
        Role = role.ToString()
    };
}
