# Trial 43: format and check say 'Not clean' where a .globalconfig's bulk severity hides StyleBro's rules.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var bulk = Migration.WithGlobalBulk(top, settings, folders);'
    Replace = 'var bulk = settings;'
    Tests = 'MigrationTests.Init_SetsStyleBroRulesById_WhereAGlobalConfigsBulkSeverityHidesThemFromFormat'
}
