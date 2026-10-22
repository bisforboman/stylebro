# Trial 42: PolyShim, a query whose project evaluation fails on an item condition is retried with a target
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (process.ExitCode != 0 && WithEmptyTarget(arguments, output + errors) is { } retry)'
    Replace = 'if (false && WithEmptyTarget(arguments, output + errors) is { } retry)'
    Tests = 'MigrationTests.Capture_AQueryOfAProjectWithAConditionOnlyItsFrameworksCanEvaluate'
}
