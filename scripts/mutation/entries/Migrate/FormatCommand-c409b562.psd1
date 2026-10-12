# format: a file that still uses one line ending isn't written again (#133)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (Ending(bytes) is not null || !bytes.Contains(Cr))'
    Replace = 'if (!bytes.Contains(Cr))'
    Tests = 'FormatLineEndingsTests.Format_KeepsEachFilesOwnLineEnding'
}
