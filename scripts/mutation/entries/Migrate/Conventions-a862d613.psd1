# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (counts[DocOrderKey].Sum() < MinimumAgreeing)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Conventions_DocOrderOfStubElementsOnly'
}
