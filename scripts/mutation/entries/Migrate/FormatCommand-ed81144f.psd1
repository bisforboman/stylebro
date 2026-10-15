# format keeps each file's own line ending when the whitespace pass writes the OS's (#133)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        RestoreLineEndings(endings, files => EndOfLineSet(root, files));'
    Replace = ''
    Tests = 'FormatLineEndingsTests.Format_KeepsEachFilesOwnLineEnding'
}
