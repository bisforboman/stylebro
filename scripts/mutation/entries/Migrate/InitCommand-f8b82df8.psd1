# Trial 43: a .globalconfig's bulk severity hides StyleBro's rules from 'dotnet format': init sets them by id.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'HiddenFromFormat(Migration.WithGlobalBulk(repository, ownSettings), styleSettings)'
    Replace = 'HiddenFromFormat(ownSettings, styleSettings)'
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereAGlobalConfigsBulkSeverityHidesThemFromFormat'
}
