# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'error.Groups["message"].Value.Contains("StyleBro.", StringComparison.OrdinalIgnoreCase)'
    Replace = 'true'
    Tests = 'MigrationTests.RestoreHint_ForAMissingStyleBroPackage'
}
