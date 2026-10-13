# Trial 14 (Bogus): a second init keeps the root's final newline decision
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (KeptLines(previous, [key]) is { Count: > 0 } keptLines)'
    Replace = 'if (KeptLines(previous, []) is { Count: > 0 } keptLines)'
    Tests = 'MigrationTests.Init_KeepsTheRootsFinalNewlineDecision'
}
