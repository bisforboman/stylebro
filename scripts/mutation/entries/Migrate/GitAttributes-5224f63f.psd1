# Backlog 085: a .gitattributes that already names the baseline is left alone
@{
    File = 'src/StyleBro.Migrate/GitAttributes.cs'
    Find = 'is { } text && NamesBaseline(text)'
    Replace = 'is { } text && false'
    Tests = 'GitAttributesTests.LeavesAFileThatNamesTheBaseline'
}
