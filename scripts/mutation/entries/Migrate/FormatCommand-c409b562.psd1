# format: a file that still uses one line ending isn't written again (#133)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(e => e.Now != e.Crlf)'
    Replace = '.Where(e => true)'
    Tests = 'FormatLineEndingsTests.Format_KeepsEachFilesOwnLineEnding'
}
