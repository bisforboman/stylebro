# Trial 53: a .nupkg not named like its folder (BaGet's TestData) doesn't make one
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'return name is not null && names.Any('
    Replace = 'return name is not null && !names.Any('
    Tests = 'MigrationTests.ACommittedPackageNextToCode_IsNotAnExtractedPackage'
}
