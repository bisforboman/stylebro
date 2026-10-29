# Only when BRO1505 is on (trial 60).
@{
    File = 'src/StyleBro.CodeFixes/Layout/DocumentationBlankLinesCodeFixProvider.cs'
    Find = 'if (!IsOn(DiagnosticIds.ElementsSeparatedByBlankLine))'
    Replace = 'if (false)'
    Tests = 'DocumentationBlankLinesTests.BlankLineBeforeDocumentation_WithBRO1505Off_AddsOnlyItsOwn'
}
