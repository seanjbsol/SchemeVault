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

public enum PhotoOwnerKind
{
    Evidence = 0,
    Accident = 1,
    Equipment = 2
}

public enum AccidentSeverity
{
    NearMiss = 0,
    MinorInjury = 1,
    LostTime = 2,
    MajorInjury = 3,
    DangerousOccurrence = 4,
    Other = 5
}

public enum AccidentStatus
{
    Open = 0,
    Closed = 1
}

public enum QuestionnaireStatus
{
    Draft = 0,
    Generated = 1
}
