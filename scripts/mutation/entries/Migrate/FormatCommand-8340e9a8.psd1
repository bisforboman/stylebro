# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (dotted.Success)'
    Replace = 'if (!dotted.Success && dotted.Success)'
    Tests = 'MigrationTests.DottedFrameworkSpellings_ShareARun'
}
