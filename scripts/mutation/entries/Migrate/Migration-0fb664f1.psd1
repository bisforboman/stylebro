# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.Folders is not null && setup.FoldersWithout.Count > 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Migrate_TurnsTheRulesOffWhereStyleCopDidntRun'
}
