# a faster first run: only the first run restores, later runs check all frameworks at once, the preview's first run reports (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'args.Contains("--no-restore") ? args : '
    Replace = ''
    Tests = 'KeptFindingsTests.Format_StillChangingAfterTheLastRun_IsNotClean'
}
