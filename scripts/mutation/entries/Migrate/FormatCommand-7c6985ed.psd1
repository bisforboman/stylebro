# dotnet format whitespace rejects diagnostic options
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (options[i] is "--diagnostics" or "--exclude-diagnostics" or "--severity")'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_WhitespacePassOnItsOwn'
}
