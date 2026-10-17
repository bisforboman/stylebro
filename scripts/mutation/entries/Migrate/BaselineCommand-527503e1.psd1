# baseline: write where the build reads (none = nothing)
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'projects.Values.Select(p => p.File).OfType<string>()'
    Replace = 'projects.Values.Select(p => p.File ?? Path.Combine(root, Baseline.FileName)).OfType<string>()'
    Tests = 'BaselineTests.Generator_WritesNothingWhenThePropsTurnTheBaselineOff'
}
