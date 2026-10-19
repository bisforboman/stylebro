@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'IsFindingLine(l) && !IsFormattingLine(l) && AsKept(l, known) is null'
    Replace = 'IsFindingLine(l) && !IsFormattingLine(l)'
    Tests = 'KeptFindingsTests.Format_AFindingItsCheckFoundAndNoFixChanged_IsNotClean'
}
