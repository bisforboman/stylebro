# Trial 48: NuGet's .nupkg.metadata marks a package folder
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (names.Any(n => n!.EndsWith(".nupkg.metadata"'
    Replace = 'if (false && names.Any(n => n!.EndsWith(".nupkg.metadata"'
    Tests = 'MigrationTests.AddIgnoredSettings_LeavesOutTheEditorConfigsOfExtractedPackages'
}
