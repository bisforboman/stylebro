# baseline: paths relative to the baseline file's folder
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'Baseline.RelativePath(baselineFolder ?? root, file)'
    Replace = 'Baseline.RelativePath(root, file)'
    Tests = 'BaselineTests.Generator_ReadsDotnetFormatsReport'
}
