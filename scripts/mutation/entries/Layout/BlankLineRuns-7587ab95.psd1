# Trial 26: the next accessor starting with its keyword or a modifier
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'if ((next.Parent is AccessorDeclarationSyntax accessor && accessor.GetFirstToken() == next)'
    Replace = 'if ((next.Parent is AccessorDeclarationSyntax accessor && false)'
    Tests = 'BlankLineRunsTests.NextAccessorWithAttributeOrModifier_NotReported'
}
