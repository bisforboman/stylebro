# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (text is null || (!always && !StyleCopSetup.ConditionalReference(text)))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.ThePackageSwap_'
}
