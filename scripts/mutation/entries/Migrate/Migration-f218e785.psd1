# Trial 59: a nearer Directory.Build.props that imports its parent shadows nothing
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'i == 0 || ImportsParentProps(above[i - 1], root)'
    Replace = 'i == 0'
    Tests = 'MigrationTests.NotLoaded_NamesTheDirectoryBuildPropsThatShadowsTheReference'
}
