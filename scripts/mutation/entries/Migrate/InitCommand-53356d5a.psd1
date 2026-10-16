# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (IdeSeverity(key, own) is { } severity ? Silent(severity) : colon'
    Replace = 'if (colon'
    Tests = 'MigrationTests.Init_AnIdeRulesOwnSeverity'
}
