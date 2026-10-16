# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'return prefer[0].Trim().Equals("true", StringComparison.OrdinalIgnoreCase) && severity'
    Replace = 'return severity'
    Tests = 'MigrationTests.Init_DoesNotAllowOneLineJumps_WhenBracesAreEnforced'
}
