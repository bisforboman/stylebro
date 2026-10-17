# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = '            RecordUnchecked(root, record.Builds);'
    Replace = ''
    Tests = 'MigrationTests.Verify_FromTheRecord_ListsTheKeptFindingsItsCheckReports_AndBuildsTheGeneratorConfigurations'
}
