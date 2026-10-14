# the repository's own IDE0055 severity stays (trial 20, MoreLINQ)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (!migrated && ownSettings.TryGetValue(IdeSeverityKey, out var ideSeverity)'
    Replace = 'if (false && ownSettings.TryGetValue(IdeSeverityKey, out var ideSeverity)'
    Tests = 'MigrationTests.Init_KeepsTheRepositorysOwnIde0055Severity'
}
