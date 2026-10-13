# format --diff: the preview's copy gets the build output of in-solution source generators.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '            FormatCommand.CopyAnalyzerOutputs(project is null ? path : Path.Combine(folder, project), source, copy);'
    Replace = ''
    Tests = 'MigrationTests.Copies_GetTheBuildOutputOfInSolutionGenerators'
}
