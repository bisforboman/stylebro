@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'checkFindings.Where(l => AsKept(l, ReadKept(keptFile, root, root)) is null && !(IsFormattingLine(l) && IsInFiles(l, keptRawStrings)))'
    Replace = 'checkFindings.Where(l => true)'
    Tests = 'KeptFindingsTests.Format_AFindingItsCheckFoundAndNoFixChanged_IsNotClean'
}
