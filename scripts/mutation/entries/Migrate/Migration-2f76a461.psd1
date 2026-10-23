# trial 48: migrate leaves enum members out for StyleCop 1.1.x
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '            lines.Add($"{StyleBro.Analyzers.Documentation.DocumentationPeriods.EnumMembersKey} = false");'
    Replace = ''
    Tests = 'MigrationTests.Migrate_LeavesEnumMembersOutOfThePeriodCheck_ForStyleCop11'
}
