# Trial 59: the shadowed reference is reported
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'shadowed.Add((Relative(above[i]), Relative(above[read - 1])));'
    Replace = ''
    Tests = 'MigrationTests.NotLoaded_NamesTheDirectoryBuildPropsThatShadowsTheReference'
}
