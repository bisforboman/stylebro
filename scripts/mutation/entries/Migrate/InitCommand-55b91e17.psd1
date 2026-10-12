# Trial 12 (2026-10-12): init follows insert_final_newline, column alignment, BRO1527, BRO1113, one-line jumps, mixed code under an unenforced key
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'KeptLines(previous, [$"dotnet_diagnostic.{enforcer}.severity"]) is { Count: > 0 } offLines'
    Replace = 'KeptLines(previous, [$"dotnet_diagnostic.{enforcer}.severity"]) is { Count: > 9999 } offLines'
    Tests = 'MigrationTests.Init_Again_KeepsARuleTurnedOffAgainstTheRepositorysKey'
}
