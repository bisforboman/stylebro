# A project with an output line compiled (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'var compiled = output.Select(l => OutputLine.Match(l)).Where(m => m.Success).Select(m => m.Groups["name"].Value)'
    Replace = 'var compiled = Enumerable.Empty<string>()'
    Tests = 'Uncompiled_CountsOutputLinesAndCompilerMessages'
}
