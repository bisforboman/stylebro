# Format speed (2026-10-14): 'would change' without a finding line isn't clean
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'kept is { Count: > 0 } && r.Lines.Any(IsFindingLine)'
    Replace = 'kept is { Count: > 0 }'
    Tests = 'MigrationTests.Format_ACheckWithOnlyTheKeptFindings_IsClean'
}
