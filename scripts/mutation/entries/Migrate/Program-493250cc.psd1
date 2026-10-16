# Trial 24 (Refit): runs everywhere except, when that list is shorter
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'folders.Count > setup.FoldersWithout.Count'
    Replace = 'false'
    Tests = 'MigrationTests.ScopeReport_NamesStyleSharp_AndTheShorterList'
}
