# Trial 31: extracted NuGet packages inside the repository are skipped
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (IsExtractedPackage(files))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.ExtractedPackages'
}
