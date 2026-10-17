# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var checkedClean = TakeCheckedClean(root) && !incomplete;'
    Replace = 'var checkedClean = TakeCheckedClean(root);'
    Tests = 'MigrationTests.Verify_RunsTheChecks_WhenTheCleanRunDidntCheckEveryRun'
}
