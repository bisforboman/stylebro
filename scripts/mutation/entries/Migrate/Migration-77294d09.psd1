# Trial 59: a reference in a Directory.Build.props a nearer one shadows
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var read = above.TakeWhile((p, i) => i == 0 || ImportsParentProps(above[i - 1], root)).Count();'
    Replace = 'var read = above.Count;'
    Tests = 'MigrationTests.NotLoaded_NamesTheDirectoryBuildPropsThatShadowsTheReference'
}
