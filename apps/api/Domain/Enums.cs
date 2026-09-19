namespace SchemeVault.Api.Domain;

public enum MembershipRole
{
    Owner = 0,
    Admin = 1,
    Member = 2
}

public enum AccreditationStatus
{
    NotStarted = 0,
    InProgress = 1,
    Active = 2,
    ExpiringSoon = 3,
    Expired = 4,
    Suspended = 5
}

public enum GapItemStatus
{
    Missing = 0,
    InProgress = 1,
    Complete = 2,
    NotApplicable = 3
}

public enum TrafficLight
{
    Grey = 0,
    Green = 1,
    Amber = 2,
    Red = 3
}
