# Trial 51: a preview below the top adds the package to the Directory.Build.props the projects import
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var scope = top is not null && IsBelow(root, top) ? Path.GetFullPath(top) : root;'
    Replace = 'var scope = root;'
    Tests = 'MigrationTests.Preview_OfAProject_AddsThePackageToTheDirectoryBuildPropsItImports'
}
