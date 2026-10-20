# MSBuild's errors don't mean the project compiled (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = ' && !m.Id.StartsWith("MSB", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'Uncompiled_CountsOutputLinesAndCompilerMessages'
}
