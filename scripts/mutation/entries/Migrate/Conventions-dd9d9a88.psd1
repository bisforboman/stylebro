# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Contradicted(convention, convention.Values[winner].Value, repository) is { } value && !IsSet(Off(convention.Enforcer!))'
    Replace = 'Contradicted(convention, convention.Values[winner].Value, repository) is { } value'
    Tests = 'MigrationTests.Conventions_AKeyTheCodeContradicts'
}
