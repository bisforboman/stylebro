# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'text.LastIndexOf("<!--", m.Index, StringComparison.Ordinal) <= text.LastIndexOf("-->", m.Index, StringComparison.Ordinal)'
    Replace = 'm.Index >= 0'
    Tests = 'MigrationTests.ThePackageSwap_'
}
