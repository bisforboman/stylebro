# Trial 8, tool (2026-10-11): the F# cause of skipped projects is named once
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (lines.Count > 0 && unsupported is { Count: > 0 })'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_SaysOnceWhyProjectsWereSkipped'
}
