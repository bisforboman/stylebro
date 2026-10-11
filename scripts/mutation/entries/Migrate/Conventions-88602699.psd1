# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'return value != code && convention.Values.Any(v => v.Value == value) && (convention.EnforcedValues?.Contains(value) ?? true) ? value : null;'
    Replace = 'return value != code ? value : null;'
    Tests = 'MigrationTests.Conventions_AKeyTheCodeContradicts'
}
