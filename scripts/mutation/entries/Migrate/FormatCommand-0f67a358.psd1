# Trial 27: findings go into the baselines of the projects compiling their file
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return (owners.Count > 0 ? owners.Select(p => p.File) : [fallback])'
    Replace = 'return ([fallback])'
    Tests = 'MigrationTests.Baseline_GoesWhereEachProjectsBuildReadsIt'
}
