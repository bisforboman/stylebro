# Trial 31: every root license holder counts as the roots own
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Any(other => holders.All(holder =>'
    Replace = '.Any(other => holders.Take(1).All(holder =>'
    Tests = 'MigrationTests.VendoredFolders_NeverTheRoot'
}
