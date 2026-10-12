# Trial 10, tool (2026-10-12): StyleBro.Analyzers in the restore output means it loads
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Contains("\"StyleBro.Analyzers/", StringComparison.OrdinalIgnoreCase)'
    Replace = 'Contains("\"StyleBro.Analyzers.Nope/", StringComparison.OrdinalIgnoreCase)'
    Tests = 'MigrationTests.Format_StyleBroLoads_'
}
