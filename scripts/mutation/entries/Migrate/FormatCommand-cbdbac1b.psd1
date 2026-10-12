# format (trial 11, ical.net): a malformed solution filter is a clear error, not a stack trace
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (IsSolutionFilter(workspacePath) && SolutionFilterError(workspacePath) is { } invalid)'
    Replace = 'if (false && SolutionFilterError(workspacePath) is { } invalid)'
    Tests = 'MigrationTests.Format_AMalformedSolutionFilter'
}
