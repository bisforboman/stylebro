# copy tracks ignored tracked files
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'foreach (var chunk in (ignoredTracked ?? []).Chunk(100))'
    Replace = 'foreach (var chunk in Array.Empty<string>().Chunk(100))'
    Tests = 'MigrationTests.PreviewCopy_TakesTrackedFilesUnderAnIgnoredFolder'
}
