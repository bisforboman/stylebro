# SDK errors don't mean the project compiled (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = ' && !m.Id.StartsWith("NETSDK", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'Uncompiled_CountsOutputLinesAndCompilerMessages'
}
