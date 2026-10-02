using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Layout;

namespace StyleBro.Analyzers.Documentation;

/// <summary>The settings for BRO1615, read from .editorconfig. Null (the rule does nothing) without a company name.</summary>
internal sealed class FileHeaderOptions
{
    public const string CompanyKey = "stylebro_file_header_company";
    public const string CopyrightKey = "stylebro_file_header_copyright";
    public const string DecorationKey = "stylebro_file_header_decoration";

    /// <summary>StyleCop's default copyright text.</summary>
    public const string DefaultCopyright = "Copyright (c) {companyName}. All rights reserved.";

    private FileHeaderOptions(string company, string copyright, string decoration)
    {
        Company = company;
        Copyright = copyright;
        Decoration = decoration;
    }

    public string Company { get; }

    /// <summary>The copyright text with '\n' for line breaks and the variables {companyName} and {fileName}.</summary>
    public string Copyright { get; }

    /// <summary>A line written above and below a new header (StyleCop's headerDecoration), or empty.</summary>
    public string Decoration { get; }

    public static FileHeaderOptions? Read(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue(CompanyKey, out var company) || string.IsNullOrWhiteSpace(company))
        {
            return null;
        }

        var copyright = options.TryGetValue(CopyrightKey, out var value) && !string.IsNullOrWhiteSpace(value) ? value : DefaultCopyright;
        options.TryGetValue(DecorationKey, out var decoration);
        return new FileHeaderOptions(company.Trim(), copyright, decoration?.Trim() ?? string.Empty);
    }

    /// <summary>The copyright text for a file: '\n' escapes become line breaks, the variables are filled in.</summary>
    public string GetCopyrightText(string fileName) =>
        Copyright.Replace("\\n", "\n").Replace("{companyName}", Company).Replace("{fileName}", fileName);
}

/// <summary>
/// Shared logic for BRO1615 (StyleCop SA1633 with an XML header, SA1634-SA1638, SA1640, SA1641): the file starts with
/// <c>// &lt;copyright file="Name.cs" company="Company"&gt;</c>, the copyright text and <c>// &lt;/copyright&gt;</c>.
/// The header is read like StyleCop reads it: the '//' comments at the top of the file up to a blank line, '//-'
/// borders left out, parsed as XML.
/// </summary>
internal static class FileHeaders
{
    /// <summary>One problem with the header: where it's reported, the message, and the edit that fixes it.</summary>
    public sealed class Finding
    {
        public Finding(int position, string message, TextChange change)
        {
            Position = position;
            Message = message;
            Change = change;
        }

        public int Position { get; }

        public string Message { get; }

        public TextChange Change { get; }
    }

    /// <summary>
    /// The header's problem, or null when it's fine or can't be fixed safely: an empty or whitespace-only file, a
    /// header in a '/* */' comment, a broken header that already has a copyright tag (a person has to repair it), and a
    /// copyright tag that shares its first or last line with other text.
    /// </summary>
    public static Finding? GetFinding(SyntaxNode root, SourceText text, FileHeaderOptions options)
    {
        var fileName = Path.GetFileName(root.SyntaxTree.FilePath);
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var first = root.GetFirstToken(includeZeroWidth: true);
        var trivia = first.LeadingTrivia;
        var index = 0;
        while (index < trivia.Count && (trivia[index].IsKind(SyntaxKind.WhitespaceTrivia) || trivia[index].IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            index++;
        }

        var lineBreak = SingleLineBlocks.LineBreak(text, 0);
        var copyrightText = options.GetCopyrightText(fileName);
        if (index == trivia.Count)
        {
            // Nothing but whitespace before the first token: no header.
            return first.IsKind(SyntaxKind.EndOfFileToken) ? null : AddHeader(trivia, index, first.SpanStart, "The file has no header", options, fileName, copyrightText, lineBreak);
        }

        if (trivia[index].IsKind(SyntaxKind.MultiLineCommentTrivia))
        {
            return null;
        }

        if (!trivia[index].IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            // A directive or a documentation comment first: no header, like StyleCop. Reported on the first code (not
            // at the start of the file like StyleCop), so a '#pragma warning disable' at the top can suppress it.
            return AddHeader(trivia, index, first.SpanStart, "The file has no header", options, fileName, copyrightText, lineBreak);
        }

        var header = GetHeaderComments(trivia, index);
        var lines = header.Where(c => !IsBorder(c)).ToList();
        var xml = Parse(lines);
        var start = (lines.Count > 0 ? lines[0] : header[0]).FullSpan.Start;
        if (xml is null)
        {
            var plain = string.Join("\n", lines.Select(c => c.ToString().Substring(2).Trim()));
            if (plain.IndexOf("<copyright", StringComparison.Ordinal) >= 0)
            {
                return null;
            }

            // A plain header with the configured text becomes the XML header; any other comment stays below it.
            if (TextMatches(plain, copyrightText))
            {
                var span = TextSpan.FromBounds(text.Lines.GetLineFromPosition(header[0].SpanStart).Start, header[header.Count - 1].Span.End);
                return new Finding(start, "The file header isn't the XML copyright header", new TextChange(span, CreateHeader("//", options, fileName, copyrightText, lineBreak)));
            }

            return AddHeader(trivia, index, start, "The file header isn't the XML copyright header", options, fileName, copyrightText, lineBreak);
        }

        var copyright = xml.Descendants("copyright").FirstOrDefault();
        if (copyright is null)
        {
            // Insert the copyright tag at the top of the header, inside any border.
            var line = text.Lines.GetLineFromPosition(lines[0].SpanStart);
            var prefix = text.ToString(TextSpan.FromBounds(line.Start, lines[0].SpanStart)) + "//";
            return new Finding(start, "The file header has no <copyright> tag", new TextChange(new TextSpan(line.Start, 0), CreateElement(prefix, options, fileName, copyrightText, lineBreak) + lineBreak));
        }

        var problems = new List<string>();
        var file = copyright.Attribute("file")?.Value;
        if (file is null)
        {
            problems.Add("has no file attribute");
        }
        else if (!string.Equals(file, fileName, StringComparison.Ordinal))
        {
            problems.Add($"file should be \"{fileName}\"");
        }

        var company = copyright.Attribute("company")?.Value;
        if (string.IsNullOrWhiteSpace(company))
        {
            problems.Add("has no company attribute");
        }
        else if (!string.Equals(company, options.Company, StringComparison.Ordinal))
        {
            problems.Add($"company should be \"{options.Company}\"");
        }

        if (string.IsNullOrWhiteSpace(copyright.Value))
        {
            problems.Add("has no copyright text");
        }
        else if (!TextMatches(copyright.Value, copyrightText))
        {
            problems.Add("text doesn't match the configured copyright text");
        }

        if (problems.Count == 0)
        {
            return null;
        }

        // The tag's lines are replaced, so the tag must have its first and last line to itself.
        var opening = lines.Where(c => c.ToString().IndexOf("<copyright", StringComparison.Ordinal) >= 0).ToList();
        if (opening.Count != 1)
        {
            return null;
        }

        var open = opening[0];
        var openText = open.ToString();
        var tag = openText.IndexOf("<copyright", StringComparison.Ordinal);
        if (openText.Substring(2, tag - 2).Trim().Length > 0)
        {
            return null;
        }

        var close = default(SyntaxTrivia);
        foreach (var comment in lines.SkipWhile(c => c != open))
        {
            var commentText = comment.ToString();
            var end = commentText.IndexOf("</copyright>", StringComparison.Ordinal);
            if (end >= 0)
            {
                close = commentText.Substring(end + "</copyright>".Length).Trim().Length == 0 ? comment : default;
                break;
            }

            if (comment == open && commentText.TrimEnd().EndsWith("/>", StringComparison.Ordinal))
            {
                close = comment;
                break;
            }
        }

        if (close.RawKind == 0)
        {
            return null;
        }

        var openLine = text.Lines.GetLineFromPosition(open.SpanStart);
        var elementPrefix = text.ToString(TextSpan.FromBounds(openLine.Start, open.SpanStart)) + "//";
        var replaced = TextSpan.FromBounds(openLine.Start, close.Span.End);
        return new Finding(
            open.SpanStart + tag,
            "The <copyright> tag " + string.Join(", ", problems),
            new TextChange(replaced, CreateElement(elementPrefix, options, fileName, copyrightText, lineBreak)));
    }

    /// <summary>The header's '//' comments, like StyleCop: up to a blank line or anything that isn't a comment.</summary>
    private static List<SyntaxTrivia> GetHeaderComments(SyntaxTriviaList trivia, int index)
    {
        var comments = new List<SyntaxTrivia>();
        var lineBreaks = 0;
        for (var i = index; i < trivia.Count; i++)
        {
            var item = trivia[i];
            if (item.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                lineBreaks = 0;
            }
            else if (item.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                lineBreaks = 0;
                comments.Add(item);
            }
            else if (item.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                if (++lineBreaks > 1)
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return comments;
    }

    private static bool IsBorder(SyntaxTrivia comment) => comment.ToString().StartsWith("//-", StringComparison.Ordinal);

    /// <summary>The header as XML, or null when it isn't XML or has no element (StyleCop's "malformed").</summary>
    private static XElement? Parse(List<SyntaxTrivia> lines)
    {
        if (lines.Count == 0)
        {
            return null;
        }

        var xml = "<root>\n" + string.Concat(lines.Select(c => c.ToString().Substring(2) + "\n")) + "</root>\n";
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            var element = XElement.Load(reader);
            return element.Descendants().Any() ? element : null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>StyleCop's comparison: the same lines, each compared without its leading and trailing whitespace.</summary>
    private static bool TextMatches(string actual, string expected)
    {
        var actualLines = Normalize(actual);
        var expectedLines = Normalize(expected);
        return actualLines.Length == expectedLines.Length
            && actualLines.Zip(expectedLines, (a, e) => string.Equals(a.Trim(), e.Trim(), StringComparison.Ordinal)).All(same => same);
    }

    private static string[] Normalize(string value) => value.Trim('\r', '\n', ' ', '\t').Replace("\r\n", "\n").Split('\n');

    /// <summary>Inserts a new header above everything, replacing the blank lines at the top of the file.</summary>
    private static Finding AddHeader(SyntaxTriviaList trivia, int index, int position, string message, FileHeaderOptions options, string fileName, string copyrightText, string lineBreak)
    {
        var end = 0;
        for (var i = 0; i < index; i++)
        {
            if (trivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                end = trivia[i].FullSpan.End;
            }
        }

        var header = CreateHeader("//", options, fileName, copyrightText, lineBreak) + lineBreak + lineBreak;
        return new Finding(position, message, new TextChange(new TextSpan(0, end), header));
    }

    private static string CreateHeader(string prefix, FileHeaderOptions options, string fileName, string copyrightText, string lineBreak)
    {
        var element = CreateElement(prefix, options, fileName, copyrightText, lineBreak);
        return options.Decoration.Length == 0
            ? element
            : prefix + " " + options.Decoration + lineBreak + element + lineBreak + prefix + " " + options.Decoration;
    }

    private static string CreateElement(string prefix, FileHeaderOptions options, string fileName, string copyrightText, string lineBreak)
    {
        var lines = new List<string> { $"{prefix} <copyright file=\"{EncodeAttribute(fileName)}\" company=\"{EncodeAttribute(options.Company)}\">" };
        foreach (var line in copyrightText.Trim('\r', '\n').Replace("\r\n", "\n").Split('\n'))
        {
            var content = new XText(line.TrimEnd()).ToString();
            lines.Add(content.Length == 0 ? prefix : prefix + " " + content);
        }

        lines.Add(prefix + " </copyright>");
        return string.Join(lineBreak, lines);
    }

    private static string EncodeAttribute(string value)
    {
        var attribute = new XAttribute("t", value).ToString();
        return attribute.Substring(3, attribute.Length - 4);
    }
}
