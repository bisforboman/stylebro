# A folder below one format leaves alone is left alone too.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Covers(x.TrimEnd(''/'') + "/", p) ||'
    Replace = ''
    Tests = 'MigrationTests.Format_DoesntSayWhitespaceRunsWhereItLeavesAFolderAlone'
}
