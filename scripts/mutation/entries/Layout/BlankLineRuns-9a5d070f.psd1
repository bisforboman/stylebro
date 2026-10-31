# Trial 64: with CSharpier, BRO1519 leaves the gap above a checked/unchecked statement alone
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'if (csharpier && next.Kind() is SyntaxKind.CheckedKeyword'
    Replace = 'if (false && next.Kind() is SyntaxKind.CheckedKeyword'
    Tests = 'BlankLineRunsTests.CheckedStatementBelowABlock_IsLeftToCSharpier'
}
