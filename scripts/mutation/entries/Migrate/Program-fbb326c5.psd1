# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '&& !lines[i].TrimStart().StartsWith(''#'')'
    Replace = ''
    Tests = 'MigrationTests.CiSteps_WithPlainVerifyNoChanges'
}
