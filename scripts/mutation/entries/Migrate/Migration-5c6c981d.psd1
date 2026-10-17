# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Concat(MarksGeneratedCode(root) ? [] : LicensedFolders(root, files)).ToList();'
    Replace = '.ToList();'
    Tests = 'MigrationTests.VendoredFolders_IncludeAFolderWithAnotherCopyrightHoldersLicense'
}
