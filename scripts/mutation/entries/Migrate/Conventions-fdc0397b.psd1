# Trial 9: files git gives each machine its ending
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& (System.Text.RegularExpressions.Regex.IsMatch(info, @"\battr/text\b") || info.Contains("w/crlf", StringComparison.Ordinal))'
    Replace = ''
    Tests = 'MigrationTests.PickedPerMachine_'
}
