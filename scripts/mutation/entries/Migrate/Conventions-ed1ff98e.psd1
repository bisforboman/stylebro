# init's static field casing: names with '_' inside are BRO1308's and don't count.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Where(n => !n.TrimStart(''_'').Contains(''_''))'
    Replace = 'Where(n => true)'
    Tests = 'FullyQualifiedName~MigrationTests.Conventions_CountPrivateConstants'
}
