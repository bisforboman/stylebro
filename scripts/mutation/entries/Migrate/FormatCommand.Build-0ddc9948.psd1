# A build that failed only on a file lock is built again (080).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'if (result.ExitCode != 0 && IsOnlyFileLock(result.Output))'
    Replace = 'if (false)'
    Tests = 'FormatCoverageTests.BuildCheck_AFileLockRace'
}
