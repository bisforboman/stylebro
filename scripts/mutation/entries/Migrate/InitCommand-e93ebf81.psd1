# no preset version for example folders outside the solutions (trial 20)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.Where(p => !IsOutsideTheBuild(root, p, solutions))'
    Replace = ''
    Tests = 'MigrationTests.Init_PinsThePreset_OnlyWhere'
}
