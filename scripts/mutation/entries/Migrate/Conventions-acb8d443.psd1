# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'var keeps = others == 0'
    Replace = 'var keeps = others >= 0'
    Tests = 'MigrationTests.TheMigrationReport_SaysWhichFilesFormatConverts'
}
