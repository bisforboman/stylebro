# format: an LF file gets LF back, a CRLF file CRLF (#133)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (bytes[i] == Lf && e.Value)'
    Replace = 'if (bytes[i] == Lf)'
    Tests = 'FormatLineEndingsTests.Format_KeepsEachFilesOwnLineEnding'
}
