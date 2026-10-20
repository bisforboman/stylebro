# format holds back the guard's IDE fixes where dotnet format ignores suppressors (trial 35)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return ids.Count > 0 && guarded && !honors() ? ids : [];'
    Replace = 'return ids.Count > 0 && !honors() ? ids : [];'
    Tests = 'MigrationTests.HeldBackIds'
}
