# Trial 25: a configuration runs only where the code parses differently than in a run already made
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'key(p, framework, configuration) is { } k && Covered(p).Add(k)'
    Replace = 'key(p, framework, configuration) is { } k'
    Tests = 'MigrationTests.Format_AddsAConfigurationRun_OnlyWhereTheCodeParsesDifferently'
}
