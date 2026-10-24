# Trial 51: and reads central package management from above it
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var packages = CentralPackages(file, scope);'
    Replace = 'var packages = CentralPackages(file, root);'
    Tests = 'MigrationTests.Preview_OfAProject_AddsThePackageToTheDirectoryBuildPropsItImports'
}
