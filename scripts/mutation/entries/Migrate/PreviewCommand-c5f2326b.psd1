# A GlobalPackageReference goes next to StyleCop's
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '(!always && !StyleCopSetup.ConditionalReference(text))'
    Replace = '!StyleCopSetup.ConditionalReference(text)'
    Tests = 'MigrationTests.ThePackageSwap_PutsAGlobalReferenceNextToStyleCops'
}
