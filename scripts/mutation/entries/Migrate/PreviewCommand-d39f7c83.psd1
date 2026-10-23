# trial 48: a folder with the packages .nupkg counts (packages.config)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase) || '
    Replace = ''
    Tests = 'MigrationTests.AddIgnoredSettings_LeavesOutTheEditorConfigsOfExtractedPackages'
}
