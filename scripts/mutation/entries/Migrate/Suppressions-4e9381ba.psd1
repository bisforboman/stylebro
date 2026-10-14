# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = '(sonar?.IsOn(m.Sonar) ?? true)'
    Replace = 'true'
    Tests = 'MigrationTests.Init_AddsNoSuppressionsForASonarRuleThatIsOff'
}
