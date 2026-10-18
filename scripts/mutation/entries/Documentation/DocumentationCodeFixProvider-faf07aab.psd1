# Trial 073 (2026-10-18): BRO1602 removes the empty lines BRO1120 would
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = 'if (removed.Count == 0)'
    Replace = 'if (true)'
    Tests = 'FixOrderTests.MisplacedDocumentation'
}
