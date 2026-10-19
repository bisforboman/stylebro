@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'IsFindingLine(l) && !IsWhitespaceLine(l) && AsKept(l, known) is null'
    Replace = 'IsFindingLine(l) && !IsWhitespaceLine(l)'
    Tests = 'KeptFindingsTests.Format_AFindingItsCheckFoundAndNoFixChanged_IsNotClean'
}
