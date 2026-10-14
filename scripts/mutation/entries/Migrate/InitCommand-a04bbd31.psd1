# only rules StyleBro's blocks turn on go off in an opt-out folder, not the team's own
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'OptOutLines(settings.Where(p => written?.Contains(p.Key, StringComparer.OrdinalIgnoreCase) == true), chain)'
    Replace = 'OptOutLines(settings, chain)'
    Tests = 'MigrationTests.NestedHiddenFromFormat_AFolderBelowAnOptOutFolder'
}
