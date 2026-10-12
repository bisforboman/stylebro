# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '&& !item.Contains(" Condition=", StringComparison.Ordinal))'
    Replace = '&& item.Contains(" Condition=", StringComparison.Ordinal))'
    Tests = 'MigrationTests.ThePackageSwap_'
}
