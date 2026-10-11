# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (body is not (BlockSyntax or IfStatementSyntax or UsingStatementSyntax))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Conventions_CountStatements'
}
