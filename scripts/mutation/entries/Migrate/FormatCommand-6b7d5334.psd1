# Mask recovery only restores characters at a line start
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (i > 0 && text[start..i].Any(c => c is not ('' '' or ''\t'')))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_PutsBackWhatAKilledRun'
}
