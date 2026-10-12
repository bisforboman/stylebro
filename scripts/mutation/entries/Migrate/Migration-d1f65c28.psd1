# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(k => k == "csharp_prefer_braces")'
    Replace = ''
    Tests = 'MigrationTests.Migrate_UnenforcedPreferBraces_'
}
