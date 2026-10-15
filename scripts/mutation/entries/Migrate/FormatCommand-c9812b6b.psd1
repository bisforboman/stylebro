# Trial 22: only directives a string of the other parse spans
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(t => spans.Any(s => s.OverlapsWith(t.Span)))'
    Replace = '.Where(t => true)'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
