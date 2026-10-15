# format and verify stop on a swapped # without record
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (DirectiveMask.Damaged(root) is { Count: > 0 } damaged)'
    Replace = 'if (DirectiveMask.Damaged(root) is { Count: > 999999 } damaged)'
    Tests = 'MigrationTests.Format_PutsBackWhatAKilledRun'
}
