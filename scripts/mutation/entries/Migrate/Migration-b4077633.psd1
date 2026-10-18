# Trial 31: an unreadable root license never leaves the root itself undecided (Hangfire)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(folder => folder != "." && csharpFolders'
    Replace = '.Where(folder => csharpFolders'
    Tests = 'MigrationTests.VendoredFolders_NeverTheRoot'
}
