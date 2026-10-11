# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'var winner = convention.KeepAny && count[1] >= MinimumAgreeing ? 1'
    Replace = 'var winner = false ? 1'
    Tests = 'MigrationTests.Conventions_KeepStatements'
}
