# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Concat(MarksGeneratedCode(root) ? [] : [.. LicensedFolders(root, files), .. UndecidedLicenseFolders(root, files)]).ToList();'
    Replace = '.Concat(MarksGeneratedCode(root) ? [] : UndecidedLicenseFolders(root, files)).ToList();'
    Tests = 'MigrationTests.VendoredFolders_IncludeAFolderWithAnotherCopyrightHoldersLicense'
}
