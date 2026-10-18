# Trial 073 (2026-10-18): BRO1602 removes the empty lines BRO1120 would: only this comment's empty lines
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = '&& line >= first && line <= last)'
    Replace = ')'
    Tests = 'FixOrderTests.MisplacedDocumentation|DocumentationTests.MisplacedDocumentation'
}
