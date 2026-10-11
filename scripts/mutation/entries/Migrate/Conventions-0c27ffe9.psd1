# Trial 8, tool (2026-10-11): the migration leaves end_of_line unset when git converts most files
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (ConvertedOutnumber(counts))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.LineEndings_'
}
