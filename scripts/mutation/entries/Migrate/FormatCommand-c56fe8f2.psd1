# Trial 30 (2026-10-18): the projects' packs, once each, sorted
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '({string.Join(", ", packs.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))})'
    Replace = '({string.Join(", ", packs)})'
    Tests = 'MigrationTests.MissingWorkloadsLine_NamesTheProjectsAndTheCommand'
}
