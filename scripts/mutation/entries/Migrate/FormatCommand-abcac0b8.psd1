@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'IsWhitespaceLine(line) || line.Contains(" IDE0055: ", StringComparison.Ordinal)'
    Replace = 'IsWhitespaceLine(line)'
    Tests = 'KeptFindingsTests.Format_AFindingItsCheckFoundAndNoFixChanged_IsNotClean'
}
