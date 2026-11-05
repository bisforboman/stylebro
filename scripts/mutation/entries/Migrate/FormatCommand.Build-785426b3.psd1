# The retry happens once (080).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'result = run();
        }'
    Replace = 'result = run();
            result = run();
        }'
    Tests = 'FormatCoverageTests.BuildCheck_AFileLockRace'
}
