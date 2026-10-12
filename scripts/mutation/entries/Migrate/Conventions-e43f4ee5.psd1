# Trial 9: grouped vs one-per-line list items
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Add(ItemsPerLineKey, Enumerable.Range(1, items.Count - 1).Any('
    Replace = 'Add(ItemsPerLineKey, Enumerable.Range(1, items.Count - 1).All('
    Tests = 'MigrationTests.Conventions_CountItemsPerLine'
}
