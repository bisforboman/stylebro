# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& !IsSet(afterBrace) && !lines.Contains(afterBrace))'
    Replace = '&& !IsSet(afterBrace))'
    Tests = 'MigrationTests.Init_TurnsBro1519Off_WhenStatementsAfterBlocksAreMixed'
}
