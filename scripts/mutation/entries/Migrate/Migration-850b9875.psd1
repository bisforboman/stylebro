# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Any(other => !other.Contains(holder, StringComparison.Ordinal) && !holder.Contains(other, StringComparison.Ordinal)))'
    Replace = '.Any(other => other != holder))'
    Tests = 'MigrationTests.VendoredFolders_IncludeAFolderWithAnotherCopyrightHoldersLicense'
}
