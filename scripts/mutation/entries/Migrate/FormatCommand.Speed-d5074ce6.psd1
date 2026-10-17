# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'if (LoadRecord(root, args) is not { Checked: true } record)'
    Replace = 'if (LoadRecord(root, args) is not { } record)'
    Tests = 'MigrationTests.Verify_FromTheRecord_ListsTheKeptFindingsItsCheckReports_AndBuildsTheGeneratorConfigurations'
}
