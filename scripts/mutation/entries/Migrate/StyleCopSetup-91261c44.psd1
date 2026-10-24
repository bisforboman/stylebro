# Trial 53: packages.config's folders, named like their .nupkg
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'package.Equals(name, StringComparison.OrdinalIgnoreCase) || '
    Replace = ''
    Tests = 'MigrationTests.ACommittedPackageNextToCode_IsNotAnExtractedPackage'
}
