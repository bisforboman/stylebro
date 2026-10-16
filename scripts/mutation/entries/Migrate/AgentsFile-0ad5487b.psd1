# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/AgentsFile.cs'
    Find = 'if (existing?.Contains(Begin, StringComparison.Ordinal) != true)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.AgentsTip_DependsOnTheFile'
}
