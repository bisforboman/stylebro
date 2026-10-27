# the remotes come along (SourceLink)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '                    GitChecked(folder, "remote", "add", remote, url.Trim());'
    Replace = ''
    Tests = 'MigrationTests.ThePreviewsCopy_StartsFromTheRepositorysHistory'
}
