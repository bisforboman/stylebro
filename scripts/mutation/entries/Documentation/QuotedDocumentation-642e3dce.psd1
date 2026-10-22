# Trial 43: only an element of plain text is compared with the solution's strings.
@{
    File = 'src/StyleBro.CodeFixes/Documentation/QuotedDocumentation.cs'
    Find = '|| element.Content.Any(c => c is not XmlTextSyntax))'
    Replace = ')'
    Tests = 'DocumentationTests.DocumentationText_TheSolutionQuotes_IsKept'
}
