# Trial 28: a conversion end_of_line asks for is a change
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => !File.Exists(f) || endOfLine.Contains(f) || !SameButEndings('
    Replace = '.Where(f => !File.Exists(f) || !SameButEndings('
    Tests = 'FormatLineEndingsTests.Verify_AFileEndOfLineConverts_IsNotClean'
}
