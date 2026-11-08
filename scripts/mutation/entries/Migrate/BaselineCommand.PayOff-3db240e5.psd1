# Backlog 086: only the entries whose line is still there go back
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.PayOff.cs'
    Find = 'entries.Where(e => StillThere(FullPath(folder, e.Key.Path), e.Key.Fingerprint))'
    Replace = 'entries'
    Tests = 'MigrationTests.PayOff_AnUnfinishedRun_PutsBackTheEntriesOfTheLinesItLeft'
}
