namespace SchemeVault.Api.Domain;

public static class TrafficLights
{
    public const int AmberWindowDays = 60;

    public static TrafficLight ForExpiry(DateTimeOffset? expiresOn, DateTimeOffset utcNow)
    {
        if (expiresOn is null)
        {
            return TrafficLight.Grey;
        }

        var days = (expiresOn.Value - utcNow).TotalDays;
        if (days < 0)
        {
            return TrafficLight.Red;
        }

        if (days <= AmberWindowDays)
        {
            return TrafficLight.Amber;
        }

        return TrafficLight.Green;
    }

    public static TrafficLight ForAccreditation(AccreditationStatus status, DateTimeOffset? expiresOn, DateTimeOffset utcNow)
    {
        if (status is AccreditationStatus.Expired or AccreditationStatus.Suspended)
        {
            return TrafficLight.Red;
        }

        if (status is AccreditationStatus.NotStarted)
        {
            return TrafficLight.Grey;
        }

        var fromExpiry = ForExpiry(expiresOn, utcNow);
        if (fromExpiry != TrafficLight.Grey)
        {
            return fromExpiry;
        }

        return status == AccreditationStatus.InProgress ? TrafficLight.Amber : TrafficLight.Green;
    }

    public static AccreditationStatus DerivedStatus(DateTimeOffset? expiresOn, DateTimeOffset utcNow, AccreditationStatus fallback)
    {
        if (expiresOn is null)
        {
            return fallback;
        }

        var days = (expiresOn.Value - utcNow).TotalDays;
        if (days < 0)
        {
            return AccreditationStatus.Expired;
        }

        if (days <= AmberWindowDays && fallback == AccreditationStatus.Active)
        {
            return AccreditationStatus.ExpiringSoon;
        }

        return fallback;
    }
}
