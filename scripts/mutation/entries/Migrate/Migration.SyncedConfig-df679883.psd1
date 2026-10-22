# The block aside (trial 44)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'Regex.Replace(File.ReadAllText(path), $"{BeginMarker}.*?{EndMarker}", string.Empty, RegexOptions.Singleline)'
    Replace = 'File.ReadAllText(path)'
    Tests = 'MigrationTests.PackageCopy'
}
