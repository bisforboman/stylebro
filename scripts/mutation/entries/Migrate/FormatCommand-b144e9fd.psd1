# Format speed (2026-10-14): named folders stay as named
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'requested.Any(r => r.EndsWith(''/'') || r.Contains(''*'', StringComparison.Ordinal))'
    Replace = 'requested.Any(r => r.Contains(''*'', StringComparison.Ordinal))'
    Tests = 'MigrationTests.Format_NamedFiles_RunOnlyWhereTheirProjectsAre'
}
