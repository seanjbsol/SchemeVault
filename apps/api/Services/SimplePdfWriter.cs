using System.Globalization;
using System.Text;

namespace SchemeVault.Api.Services;

/// <summary>
/// Minimal PDF writer for SchemeVault draft packs. Helvetica, A4, wrapped text.
/// Avoids a third-party PDF licence for this scaffold.
/// </summary>
public static class SimplePdfWriter
{
    public static byte[] FromPlainText(string title, string body)
    {
        var lines = Wrap(StripMarkdown(body), 92);
        const int linesPerPage = 48;
        var pages = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += linesPerPage)
        {
            pages.Add(lines.GetRange(i, Math.Min(linesPerPage, lines.Count - i)));
        }

        if (pages.Count == 0)
        {
            pages.Add(["(empty document)"]);
        }

        var pageCount = pages.Count;
        var pageStart = 4;
        var contentStart = pageStart + pageCount;
        var pageRefs = string.Join(" ", Enumerable.Range(0, pageCount).Select(i => $"{pageStart + i} 0 R"));

        var objects = new List<byte[]> { Array.Empty<byte>() }; // 1-based
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{pageRefs}] /Count {pageCount} >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        for (var i = 0; i < pageCount; i++)
        {
            var contentNo = contentStart + i;
            objects.Add(Encoding.ASCII.GetBytes(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents {contentNo} 0 R /Resources << /Font << /F1 3 0 R >> >> >>"));
        }

        for (var i = 0; i < pageCount; i++)
        {
            objects.Add(ContentStream(title, pages[i], i + 1, pageCount));
        }

        using var output = new MemoryStream();
        output.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        var offsets = new long[objects.Count];
        for (var number = 1; number < objects.Count; number++)
        {
            offsets[number] = output.Position;
            var header = Encoding.ASCII.GetBytes($"{number} 0 obj\n");
            var footer = Encoding.ASCII.GetBytes("\nendobj\n");
            output.Write(header);
            output.Write(objects[number]);
            output.Write(footer);
        }

        var xrefOffset = output.Position;
        var xref = new StringBuilder();
        xref.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Count}\n");
        xref.Append("0000000000 65535 f \n");
        for (var number = 1; number < objects.Count; number++)
        {
            xref.Append(CultureInfo.InvariantCulture, $"{offsets[number]:0000000000} 00000 n \n");
        }

        xref.Append("trailer\n");
        xref.Append(CultureInfo.InvariantCulture, $"<< /Size {objects.Count} /Root 1 0 R >>\n");
        xref.Append("startxref\n");
        xref.Append(xrefOffset.ToString(CultureInfo.InvariantCulture));
        xref.Append("\n%%EOF\n");
        output.Write(Encoding.ASCII.GetBytes(xref.ToString()));
        return output.ToArray();
    }

    private static byte[] ContentStream(string title, List<string> lines, int page, int pageCount)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n/F1 11 Tf\n72 800 Td\n");
        sb.Append(PdfString($"{title}  —  page {page} of {pageCount}"));
        sb.Append(" Tj\n0 -22 Td\n/F1 10 Tf\n");
        var first = true;
        foreach (var line in lines)
        {
            if (!first)
            {
                sb.Append("0 -14 Td\n");
            }

            first = false;
            sb.Append(PdfString(line.Length == 0 ? " " : line));
            sb.Append(" Tj\n");
        }

        sb.Append("ET\n");
        var stream = Encoding.Latin1.GetBytes(sb.ToString());
        var header = Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream");
        return header.Concat(stream).Concat(footer).ToArray();
    }

    private static string PdfString(string text)
    {
        var latin = Encoding.Latin1;
        var bytes = latin.GetBytes(text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '));
        var sb = new StringBuilder();
        sb.Append('(');
        foreach (var b in bytes)
        {
            var c = (char)b;
            if (c is '(' or ')' or '\\')
            {
                sb.Append('\\').Append(c);
            }
            else if (b < 32 || b > 126)
            {
                sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            }
            else
            {
                sb.Append(c);
            }
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string StripMarkdown(string markdown)
    {
        var sb = new StringBuilder();
        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd().TrimStart('#', ' ').Replace("**", string.Empty);
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static List<string> Wrap(string text, int width)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                result.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                    continue;
                }

                if (current.Length + 1 + word.Length > width)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
                else
                {
                    current.Append(' ').Append(word);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }
        }

        return result;
    }
}
