# Trial 43: documentation text a snapshot file holds is kept.
@{
    File = 'src/StyleBro.CodeFixes/Documentation/QuotedDocumentation.cs'
    Find = 'return names.Files.Texts.FirstOrDefault(f => f.Text.IndexOf(text, StringComparison.Ordinal) >= 0).Path;'
    Replace = 'return null;'
    Tests = 'DocumentationTests.DocumentationText_TheSolutionQuotes_IsKept'
}
