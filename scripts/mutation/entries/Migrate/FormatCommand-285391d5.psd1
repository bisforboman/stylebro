# Trial 22: .NET Framework is a kind of its own
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Regex.IsMatch(name, @"^net\d+\.\d") ? "net"'
    Replace = 'Regex.IsMatch(name, @"^net\d") ? "net"'
    Tests = 'MigrationTests.Format_RunsAFrameworkOnlyWhereItsCodeCompilesDifferently'
}
