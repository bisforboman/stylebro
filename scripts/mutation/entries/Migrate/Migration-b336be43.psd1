# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'setup.IsOn("SA1503") || setup.IsOn("SA1519") || setup.IsOn("SA1520") ? "true"'
    Replace = 'setup.IsOn("SA1503") ? "true"'
    Tests = 'MigrationTests.Migrate_Braces_'
}
