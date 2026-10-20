@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var unfixed = changed.Count == 0 ? checkFindings'
    Replace = 'var unfixed = changed.Count < 0 ? checkFindings'
    Tests = 'KeptFindingsTests.Format_AFindingItsCheckFoundAndNoFixChanged_IsNotClean'
}
