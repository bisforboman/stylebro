# Trial 22: BRO1602's fix adds BRO1504's blank line.
@{
    File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'
    Find = 'edits, options, cancellationToken) is { } blankLine)'
    Replace = 'edits, options, cancellationToken) is { } blankLine && false)'
    Tests = 'DocumentationTests.TripleSlashComments_BecomePlainComments'
}
