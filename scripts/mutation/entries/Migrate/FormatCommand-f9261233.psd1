# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'checkedClean && changed.Count == 0 ? CleanVerdict(root, keptFindings, heldBackNotes) : null'
    Replace = 'checkedClean ? CleanVerdict(root, keptFindings, heldBackNotes) : null'
    Tests = 'MigrationTests.Verify_RunsTheChecks_AfterARunThatChangedLineEndingsOnly'
}
