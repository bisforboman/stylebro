# Trial 31: extracted NuGet packages inside the repository are skipped
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (files.Any(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.ExtractedPackages'
}
