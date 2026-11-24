using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;

namespace MercuryOutpost.Web.Components;

/// <summary>
/// Colours short C# snippets the way VS Code's Dark+ theme does, without a client-side library.
/// Good enough for the few lines shown on the Plumbing page; not a real C# lexer.
/// </summary>
public static class CSharpHighlighter
{
    private static readonly HashSet<string> Keywords =
        ["new", "var", "string", "int", "bool", "using", "true", "false", "null", "this", "async"];

    private static readonly HashSet<string> ControlKeywords =
        ["await", "return", "if", "else", "foreach", "while"];

    public static MarkupString Highlight(string code) =>
        new(string.Join("\n", code.Split('\n').Select(HighlightLine)));

    private static string HighlightLine(string line)
    {
        var html = new StringBuilder();
        string? previous = null;   // last token that was not whitespace
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];

            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                Append(html, "comment", line[i..]);
                break;
            }

            if (c == '"')
            {
                int end = line.IndexOf('"', i + 1);
                end = end < 0 ? line.Length - 1 : end;
                Append(html, "str", line[i..(end + 1)]);
                previous = "\"";
                i = end + 1;
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                string word = line[start..i];

                int next = i;
                while (next < line.Length && line[next] == ' ') next++;
                char after = next < line.Length ? line[next] : '\0';

                Append(html, Classify(word, previous, after), word);
                previous = word;
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i;
                while (i < line.Length && char.IsDigit(line[i])) i++;
                Append(html, "num", line[start..i]);
                previous = "0";
                continue;
            }

            html.Append(HtmlEncoder.Default.Encode(c.ToString()));
            if (!char.IsWhiteSpace(c)) previous = c.ToString();
            i++;
        }

        string result = html.ToString();
        return line.StartsWith("IChatClient ", StringComparison.Ordinal)
            ? $"<span class=\"result\">{result}</span>"
            : result;
    }

    private static string Classify(string word, string? previous, char after)
    {
        if (ControlKeywords.Contains(word)) return "ctrl";
        if (Keywords.Contains(word)) return "kw";
        if (previous == "new") return "type";                     // constructor call
        if (after == '(') return "method";
        if (after == ':') return "var";                           // named argument
        if (previous == "." || after == '=') return char.IsUpper(word[0]) ? "prop" : "var";
        return char.IsUpper(word[0]) ? "type" : "var";
    }

    private static void Append(StringBuilder html, string cls, string text) =>
        html.Append("<span class=\"").Append(cls).Append("\">").Append(HtmlEncoder.Default.Encode(text)).Append("</span>");
}
