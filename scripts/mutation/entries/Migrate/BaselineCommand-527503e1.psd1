# baseline: write where the build reads (none = nothing)
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'if (FormatCommand.BaselineFile(root) is not { } path)'
    Replace = 'if (Path.Combine(root, Baseline.FileName) is not { } path)'
    Tests = 'BaselineTests.Generator_WritesNothingWhenThePropsTurnTheBaselineOff'
}
