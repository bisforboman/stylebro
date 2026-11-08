# Backlog 086: after a run that did not end clean, the entries for the lines it left go back
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.PayOff.cs'
    Find = 'if (code != 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.PayOff_AnUnfinishedRun_PutsBackTheEntriesOfTheLinesItLeft'
}
