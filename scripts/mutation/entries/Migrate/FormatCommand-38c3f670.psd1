# Trial 22: another analyzer's rule without a fix is no failure
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        return Regex.Match(line, @"Unable to fix (?<id>BRO\d+)\. (?<error>.*)$")'
    Replace = '        return Regex.Match(line, @"Unable to fix (?<id>\w+)\. (?<error>.*)$")'
    Tests = 'MigrationTests.Format_IsNotClean_WhenACodeFixFailed'
}
