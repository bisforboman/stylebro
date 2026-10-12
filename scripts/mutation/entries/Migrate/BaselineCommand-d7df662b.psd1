# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)'
    Replace = 'true'
    Tests = 'MigrationTests.Baseline_TakesASolutionOrProjectFirst'
}
