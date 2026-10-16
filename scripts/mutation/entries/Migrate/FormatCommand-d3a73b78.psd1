# Trial 8, tool (2026-10-11): 'Not clean' lists the kept findings too
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'KeptSummary(keptFindings, shown, clean: false).ForEach(log);'
    Replace = ''
    Tests = 'KeptFindingsTests.Format_StillChangingAfterTheLastRun_IsNotClean'
}
