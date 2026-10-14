# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (main.KeptUsings.Count > 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Migrate_NamesFilesNotInTheOrder'
}
