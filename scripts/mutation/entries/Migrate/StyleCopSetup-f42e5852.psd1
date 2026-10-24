# Trial 53: a global packages folder's id/version folders
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = ' || package.Equals($"{parent}.{name}", StringComparison.OrdinalIgnoreCase)'
    Replace = ''
    Tests = 'MigrationTests.ACommittedPackageNextToCode_IsNotAnExtractedPackage'
}
