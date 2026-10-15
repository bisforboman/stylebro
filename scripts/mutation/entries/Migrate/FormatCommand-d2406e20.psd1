# Trial 22/23: every directive counts, also a string's own '#if'/'#endif' lines
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(t => t.IsDirective).Select(t => text.IndexOf(''#'', t.Span.Start))'
    Replace = '.Where(t => t.IsDirective && !t.ToString().Contains("if", StringComparison.Ordinal)).Select(t => text.IndexOf(''#'', t.Span.Start))'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
