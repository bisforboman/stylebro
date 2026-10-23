# trial 48: format records the files each run undoes, for check
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '                RememberUndone(root, undone);
'
    Replace = ''
    Tests = 'FormatCoverageTests.Format_NamesTheFilesEachRunUndoes'
}
