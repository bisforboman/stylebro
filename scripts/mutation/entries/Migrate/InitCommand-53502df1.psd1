# Trial 14: a second init keeps the spacing lines
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var keptSpacing = KeptSpacing(previous);'
    Replace = 'var keptSpacing = new List<string>();'
    Tests = 'MigrationTests.Init_KeepsTheSpacingDecision'
}
