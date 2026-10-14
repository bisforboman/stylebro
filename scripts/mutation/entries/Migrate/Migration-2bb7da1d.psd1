# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (enforcers.Count == 0)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Migrate_NamesFilesNotInTheOrder'
}
