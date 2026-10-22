# Trial 42: kiota-dotnet, the preview names the configurations an in-solution generator keeps from the runs
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'gaps.AddRange(FormatCommand.GeneratorGaps(formatRoot)'
    Replace = '_ = (FormatCommand.GeneratorGaps(formatRoot)'
    Tests = 'MigrationTests.Preview_NamesTheConfigurationsAnInSolutionGeneratorKeepsFromTheRuns'
}
