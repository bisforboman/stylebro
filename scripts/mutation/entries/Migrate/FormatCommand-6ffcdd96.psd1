# Mask recovery leaves a running format's record alone
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (ReadRecord(file) is not { } leftover || IsRunning(leftover))'
    Replace = 'if (ReadRecord(file) is not { } leftover)'
    Tests = 'MigrationTests.Format_PutsBackWhatAKilledRun'
}
