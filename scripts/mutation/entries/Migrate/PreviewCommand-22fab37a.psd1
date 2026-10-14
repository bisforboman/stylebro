# copy tracks ignored tracked files
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'GitChecked(folder, ["add", "-f", "--", .. chunk]);'
    Replace = ''
    Tests = 'FullyQualifiedName~MigrationTests.PreviewCopy_TakesTrackedFilesUnderAnIgnoredFolder'
}
