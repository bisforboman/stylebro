# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'decided[convention.Key] = $"your .editorconfig enforces braces ({braces})";'
    Replace = '_ = braces;'
    Tests = 'MigrationTests.Init_DoesNotAllowOneLineJumps_WhenBracesAreEnforced'
}
