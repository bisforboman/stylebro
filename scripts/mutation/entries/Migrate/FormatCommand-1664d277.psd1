# The kept findings the verdict lists wait with it.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (verdicts.Count > 0 || line.StartsWith("Clean", StringComparison.Ordinal) || (buildAll'
    Replace = 'if (line.StartsWith("Clean", StringComparison.Ordinal) || (buildAll'
    Tests = 'KeptFindingsTests.Format_WithAVerdictThatWaits_KeepsTheKeptFindingsBelowIt'
}
