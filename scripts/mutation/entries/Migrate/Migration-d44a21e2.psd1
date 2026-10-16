# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return owner is not null && compiled.TryGetValue(owner, out var items) && items is not null && !items.Contains(full);'
    Replace = 'return owner is not null && compiled.TryGetValue(owner, out var items) && items is not null;'
    Tests = 'MigrationTests.Init_CountsOnlyCompiledFiles'
}
