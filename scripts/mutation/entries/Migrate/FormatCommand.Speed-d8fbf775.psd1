@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'IsFindingLine(l) && !IsLineEndingLine(l) && AsKept(l, known) is null'
    Replace = 'IsFindingLine(l) && !IsLineEndingLine(l)'
    Tests = 'KeptFindingsTests.Format_AFindingItsCheckFoundAndNoFixChanged_IsNotClean'
}
