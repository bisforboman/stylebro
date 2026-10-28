# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var checkedClean = TakeCheckedClean(root) && !incomplete && gaps.All(IsHeldBackGap);'
    Replace = 'var checkedClean = TakeCheckedClean(root) && gaps.All(IsHeldBackGap);'
    Tests = 'MigrationTests.Verify_RunsTheChecks_WhenTheCleanRunDidntCheckEveryRun'
}
