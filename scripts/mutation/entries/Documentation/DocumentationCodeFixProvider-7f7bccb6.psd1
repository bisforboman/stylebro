# Trial 073 (2026-10-18): BRO1602 removes the empty lines BRO1120 would (only while BRO1120 is on)
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = '|| !Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.EmptyComment, cancellationToken))'
    Replace = ')'
    Tests = 'DocumentationTests.MisplacedDocumentation_EmptyEndLines'
}
