namespace SchemeVault.Api.Billing;

public static class PlanFeatures
{
    public const string Questionnaires = "questionnaires";
    public const string MultiSchemeExport = "multi_scheme_export";
    public const string Accidents = "accidents";
    public const string Equipment = "equipment";

    public static readonly string[] Pro =
    [
        Questionnaires,
        MultiSchemeExport,
        Accidents,
        Equipment
    ];
}

public static class EntitlementsNormalizer
{
    public const string HttpItemKey = "SchemeVault.Entitlements";

    public static EntitlementsDto Apply(EntitlementsDto dto, SubscriptionApiOptions options)
    {
        var features = (dto.Features ?? [])
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var isPro = options.ForcePro || LooksLikePro(dto.Plan) || LooksLikePro(dto.PlanCode) ||
                    features.Any(f => string.Equals(f, "pro", StringComparison.OrdinalIgnoreCase));

        if (isPro)
        {
            foreach (var feature in PlanFeatures.Pro)
            {
                if (!features.Exists(f => string.Equals(f, feature, StringComparison.OrdinalIgnoreCase)))
                {
                    features.Add(feature);
                }
            }

            if (string.IsNullOrWhiteSpace(dto.Plan) || LooksLikeStarter(dto.Plan))
            {
                dto.Plan = "Pro";
            }

            if (string.IsNullOrWhiteSpace(dto.PlanCode) || LooksLikeStarter(dto.PlanCode))
            {
                dto.PlanCode = "pro";
            }
        }

        dto.Features = features.ToArray();
        dto.IsPro = isPro;
        dto.IsActive = dto.IsActiveOrTrialing;
        return dto;
    }

    public static bool HasFeature(EntitlementsDto dto, string? feature)
    {
        if (dto.IsPro)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(feature))
        {
            return false;
        }

        return dto.Features.Any(f => string.Equals(f, feature, StringComparison.OrdinalIgnoreCase));
    }

    public static bool LooksLikePro(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Equals("pro", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("schemevault_pro", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("schemevault-pro", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("pro", StringComparison.OrdinalIgnoreCase) &&
         !LooksLikeStarter(value));

    public static bool LooksLikeStarter(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Equals("starter", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("schemevault_starter", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("schemevault-starter", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("starter", StringComparison.OrdinalIgnoreCase));
}
