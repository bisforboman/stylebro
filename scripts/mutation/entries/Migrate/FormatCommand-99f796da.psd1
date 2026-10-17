# Trial 28: old-style projects get a run next to multi-targeted ones
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'p.Value.Length == 0 ? [(Framework: string.Empty, Project: p.Key)]'
    Replace = 'p.Value.Length < 0 ? [(Framework: string.Empty, Project: p.Key)]'
    Tests = 'MigrationTests.Format_GivesOldStyleProjectsARun_NextToMultiTargetedOnes'
}
