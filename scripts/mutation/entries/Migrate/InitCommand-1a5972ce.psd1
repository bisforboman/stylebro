# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '&& ownSettings.TryGetValue(key, out var teams) && teams.ToLowerInvariant() is "suggestion" or "warning" or "error")'
    Replace = '&& ownSettings.TryGetValue(key, out var teams) && teams.ToLowerInvariant() is "x")'
    Tests = 'MigrationTests.Init_SaysWhenTheBlockTurnsOff'
}
