# BRO1519: a } followed by a token continuing its expression or pattern isn't the end of a block
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'is ExpressionSyntax or PatternSyntax'
    Replace = 'is CompilationUnitSyntax'
    Tests = 'BlankLineRunsTests'
}
