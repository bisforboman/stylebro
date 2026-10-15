# Trial of 0.5.0-alpha.1 (2026-10-10): old Mac line endings: a CR followed by LF isn't one
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (at + 1 < text.Length && text[at + 1] == ''\n'')'
    Replace = 'if (at + 1 < text.Length && text[at + 1] == ''x'')'
    Tests = 'MigrationTests.Format_NamesFilesWithOldMacLineEndings'
}
