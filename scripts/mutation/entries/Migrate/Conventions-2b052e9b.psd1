# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'trees.Where(t => only?.Contains(t) ?? true)'
    Replace = 'trees'
    Tests = 'MigrationTests.Conventions_CountTheMembersBro1601Checks'
}
