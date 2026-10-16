# Trial 22: a .globalconfig's IDE0055 by id beats the bulk severity
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': set.GlobalConfigOptions.TreeOptions.TryGetValue(id, out var global) ? global'
    Replace = ': set.GlobalConfigOptions.TreeOptions.TryGetValue(id + " ", out var global) ? global'
    Tests = 'MigrationTests.Format_HonoursTheRepositorysOwnIDE0055Severity'
}
