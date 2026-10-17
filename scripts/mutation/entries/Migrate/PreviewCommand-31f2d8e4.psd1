# Trial 28: the preview keeps each file's line ending
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'format = (a, o) => keepingEndings(a, o);'
    Replace = 'format = (a, o) => formatOnce(a, o);'
    Tests = 'MigrationTests.Preview_KeepsEachFilesLineEnding'
}
