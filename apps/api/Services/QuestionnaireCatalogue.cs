using SchemeVault.Api.Contracts;

namespace SchemeVault.Api.Services;

public sealed record QuestionnaireDefinition(
    string SchemeCode,
    string SchemeName,
    string Title,
    string Introduction,
    IReadOnlyList<QuestionnaireQuestionDto> Questions);

public static class QuestionnaireCatalogue
{
    public const string Disclaimer =
        "This is a working draft produced from answers given in SchemeVault. It is not an official CHAS, Constructionline, SafeContractor, Avetta or SMAS submission, not a certificate of compliance, and not legal advice. A competent person should review it before anything is pasted into a scheme portal.";

    public static IReadOnlyList<QuestionnaireDefinition> All { get; } =
    [
        Define("CHAS", "CHAS", "CHAS pack questionnaire",
            "Plain-English questions that assemble a CHAS-style health and safety pack draft. SchemeVault does not submit to Alcumus CHAS."),
        Define("CONSTRUCTIONLINE", "Constructionline", "Constructionline pack questionnaire",
            "Questions aimed at Constructionline-style pre-qualification drafts (organisation, insurance, H&S). SchemeVault does not submit to Constructionline."),
        Define("SAFECONTRACTOR", "SafeContractor", "SafeContractor pack questionnaire",
            "Questions for a SafeContractor-style evidence narrative. SchemeVault does not submit to Alcumus SafeContractor."),
        Define("AVETTA", "Avetta", "Avetta pack questionnaire",
            "Questions for a client-portal style Avetta draft. Each client account still needs its own portal answers; this is a reusable narrative only."),
        Define("SMAS", "SMAS Worksafe", "SMAS Worksafe pack questionnaire",
            "Questions for an SMAS Worksafe-style pack draft. Mutual recognition does not replace a portal submission.")
    ];

    public static QuestionnaireDefinition? Find(string schemeCode) =>
        All.FirstOrDefault(d => d.SchemeCode.Equals(schemeCode, StringComparison.OrdinalIgnoreCase));

    private static QuestionnaireDefinition Define(string code, string name, string title, string intro) =>
        new(code, name, title, intro, CoreQuestions(code).ToList());

    private static IEnumerable<QuestionnaireQuestionDto> CoreQuestions(string schemeCode)
    {
        yield return Q("companyName", "What is the legal name of the company?",
            "As registered at Companies House, not a trading nickname.");
        yield return Q("companiesHouseNumber", "Companies House number (if you have one)",
            "Optional. Eight characters, for example 01234567.", "text", false);
        yield return Q("tradingAddress", "Main trading address",
            "Include postcode. This is the address scheme assessors usually match to insurance.");
        yield return Q("headcount", "Roughly how many people work for you, including directors who go to site?",
            "A number is enough. Agency labour can be mentioned in the notes below.", "number");
        yield return Q("workDescription", "In a sentence or two, what work do you mainly do?",
            "For example plant hire, groundworks, electrical installation, scaffolding.", "longText");
        yield return Q("hsResponsible", "Who is responsible for health and safety?",
            "Name and job title. If you use an external adviser, say so here.");
        yield return Q("hsPolicy", "Do you have a written health and safety policy signed in the last 12 months?",
            "Yes or no is fine. If yes, say who signed it and roughly when.", "yesNo");
        yield return Q("elInsurance", "Employers' Liability insurance — insurer and expiry",
            "Example: Aviva, expires 31 March 2027. SchemeVault does not check the certificate for you.");
        yield return Q("plInsurance", "Public Liability insurance — insurer, limit and expiry",
            "Example: Hiscox, £10 million, expires 31 March 2027.");
        yield return Q("competence", "How do you make sure people are trained and competent for the job?",
            "CSCS/CPCS (or equivalent), toolbox talks, a training matrix — write it as you would tell a new supervisor.", "longText");
        yield return Q("rams", "How do you produce RAMS for a typical job?",
            "Who writes them, who signs them, and how a site team actually sees them.", "longText");
        yield return Q("accidents", "How do you report accidents, including RIDDOR where it applies?",
            "Who is told first, where it is recorded, and who decides whether it is reportable. This is not a RIDDOR filing service.", "longText");
        yield return Q("firstAid", "What first-aid arrangements do you have?",
            "Named first-aiders if you have them, and where kits live.");
        yield return Q("subcontractors", "If you use subcontractors, how do you check they are competent?",
            "If you never use them, say so. Otherwise: insurance, RAMS, and who signs them onto site.", "longText", false);
        yield return Q("enforcement", "Any HSE improvement/prohibition notices or prosecutions in the last five years?",
            "Yes or no. If yes, say what you changed afterwards. Honesty matters more than a perfect record.", "yesNo");
        yield return Q("otherNotes", "Anything else an assessor should know?",
            "Optional. Recent ISO work, SSIP certificates you already hold, or a contract that drives this pack.", "longText", false);

        switch (schemeCode.ToUpperInvariant())
        {
            case "CHAS":
                yield return Q("ssipExisting", "Do you already hold another SSIP member scheme?",
                    "If yes, name it and the expiry. Mutual recognition can reduce duplication; it does not replace the CHAS portal.", "yesNo", false);
                break;
            case "CONSTRUCTIONLINE":
                yield return Q("workCategories", "Which Constructionline-style work categories do you typically tender for?",
                    "Free text is fine (for example 451000 groundworks). This draft does not set your notation.", "longText", false);
                break;
            case "SAFECONTRACTOR":
                yield return Q("sectorModules", "Any higher-risk activities (work at height, asbestos, lifting) you want called out?",
                    "Assessors often ask for extra modules. List what you actually do, not what you might do.", "longText", false);
                break;
            case "AVETTA":
                yield return Q("clientAccount", "Which client portal is this draft for, if you know?",
                    "Avetta is client-specific. Naming the client helps you reuse the narrative; SchemeVault does not log into Avetta.", "text", false);
                break;
            case "SMAS":
                yield return Q("ssipExisting", "Do you already hold another SSIP member scheme?",
                    "If yes, name it. SMAS may still want its own documents even when Deem to Satisfy applies.", "yesNo", false);
                break;
        }
    }

    private static QuestionnaireQuestionDto Q(
        string id,
        string prompt,
        string help,
        string kind = "text",
        bool required = true) =>
        new()
        {
            Id = id,
            Prompt = prompt,
            Help = help,
            Kind = kind,
            Required = required
        };
}
