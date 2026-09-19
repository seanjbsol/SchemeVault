using System.Globalization;
using System.Text;
using SchemeVault.Api.Contracts;

namespace SchemeVault.Api.Services;

public static class QuestionnaireDocument
{
    public static string Render(
        QuestionnaireDefinition definition,
        string organisationName,
        IReadOnlyDictionary<string, string?> answers,
        DateTimeOffset generatedAt)
    {
        var uk = generatedAt.ToOffset(TimeSpan.FromHours(1)); // BST-ish display; labelled as UTC below if needed
        var when = generatedAt.ToString("d MMMM yyyy, HH:mm 'UTC'", CultureInfo.GetCultureInfo("en-GB"));
        var sb = new StringBuilder();
        sb.AppendLine($"# {definition.SchemeName} compliance pack draft");
        sb.AppendLine();
        sb.AppendLine($"**Organisation:** {organisationName}");
        sb.AppendLine($"**Prepared:** {when}");
        sb.AppendLine("**Prepared in:** SchemeVault (working draft)");
        sb.AppendLine();
        sb.AppendLine("## Disclaimer");
        sb.AppendLine();
        sb.AppendLine(QuestionnaireCatalogue.Disclaimer);
        sb.AppendLine();
        sb.AppendLine("## Organisation");
        sb.AppendLine();
        Line(sb, "Legal name", answers, "companyName");
        Line(sb, "Companies House number", answers, "companiesHouseNumber");
        Line(sb, "Trading address", answers, "tradingAddress");
        Line(sb, "Headcount (including directors on site)", answers, "headcount");
        Line(sb, "Main work", answers, "workDescription");
        sb.AppendLine();
        sb.AppendLine("## Health and safety management");
        sb.AppendLine();
        Line(sb, "Person responsible", answers, "hsResponsible");
        Line(sb, "Written H&S policy (signed in last 12 months)", answers, "hsPolicy");
        sb.AppendLine();
        sb.AppendLine("## Insurance");
        sb.AppendLine();
        Line(sb, "Employers' Liability", answers, "elInsurance");
        Line(sb, "Public Liability", answers, "plInsurance");
        sb.AppendLine();
        sb.AppendLine("## Competence and RAMS");
        sb.AppendLine();
        Line(sb, "Training and competence", answers, "competence");
        Line(sb, "How RAMS are produced", answers, "rams");
        sb.AppendLine();
        sb.AppendLine("## Accidents and first aid");
        sb.AppendLine();
        Line(sb, "Accident / RIDDOR process", answers, "accidents");
        Line(sb, "First aid", answers, "firstAid");
        sb.AppendLine();
        sb.AppendLine("## Subcontractors and enforcement");
        sb.AppendLine();
        Line(sb, "Subcontractor checks", answers, "subcontractors");
        Line(sb, "Notices or prosecutions (last five years)", answers, "enforcement");
        Line(sb, "Other notes", answers, "otherNotes");

        var extras = definition.Questions.Where(q =>
            q.Id is "ssipExisting" or "workCategories" or "sectorModules" or "clientAccount").ToList();
        if (extras.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"## {definition.SchemeName}-specific notes");
            sb.AppendLine();
            foreach (var question in extras)
            {
                Line(sb, question.Prompt, answers, question.Id);
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Answers given");
        sb.AppendLine();
        foreach (var question in definition.Questions)
        {
            var value = Read(answers, question.Id);
            sb.AppendLine($"**{question.Prompt}**");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(value) ? "_Not answered._" : value);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void Line(StringBuilder sb, string label, IReadOnlyDictionary<string, string?> answers, string id)
    {
        var value = Read(answers, id);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.AppendLine($"- **{label}:** {value}");
    }

    private static string Read(IReadOnlyDictionary<string, string?> answers, string id) =>
        answers.TryGetValue(id, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : string.Empty;
}
