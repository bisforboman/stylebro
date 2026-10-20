# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var checkedClean = TakeCheckedClean(root) && !incomplete && gaps.Count == 0;'
    Replace = 'var checkedClean = TakeCheckedClean(root) && gaps.Count == 0;'
    Tests = 'MigrationTests.Verify_RunsTheChecks_WhenTheCleanRunDidntCheckEveryRun'
}
