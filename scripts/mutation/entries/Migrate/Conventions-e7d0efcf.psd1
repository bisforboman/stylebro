# Trial 8, tool (2026-10-11): init leaves end_of_line unset when git converts most files
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'else if (convention.Key == EndOfLineKey && ConvertedOutnumber(counts))'
    Replace = 'else if (false)'
    Tests = 'MigrationTests.LineEndings_'
}
