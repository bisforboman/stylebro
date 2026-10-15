# A running format's swapped files are not damaged
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& !live.Contains(Path.GetFullPath(f)))'
    Replace = ')'
    Tests = 'MigrationTests.Format_PutsBackWhatAKilledRun'
}
