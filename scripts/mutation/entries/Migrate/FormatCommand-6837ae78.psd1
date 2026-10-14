# no empty --exclude
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        if (exclusions.Count == 0)
        {
            return result;
        }'
    Replace = ''
    Tests = 'MigrationTests.Baseline_RunsEachPassOnItsOwn'
}
