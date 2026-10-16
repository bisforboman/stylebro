# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '("none", "right below the ''}''"), Off(DiagnosticIds.BlankLineAfterCloseBrace))'
    Replace = '("none", "right below the ''}''"), null)'
    Tests = 'MigrationTests.Init_TurnsBro1519Off_WhenStatementsAfterBlocksAreMixed'
}
