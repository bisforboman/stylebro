# Whitespace exclusions: IDE0055's category or bulk severity counts (trial 22: the repository's own settings too)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': Severities.Configured(options.AnalyzerOptions, id, "Style") ??'
    Replace = ': null ??'
    Tests = 'MigrationTests.Format_HonoursTheRepositorysOwnIDE0055Severity'
}
