# a shallow repository's boundary comes along
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '            File.Copy(shallow, Path.Combine(folder, ".git", "shallow"), overwrite: true);'
    Replace = ''
    Tests = 'MigrationTests.ThePreviewsCopy_StartsFromTheRepositorysHistory'
}
