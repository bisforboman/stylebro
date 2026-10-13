# BRO1519: no blank line between an accessor list and the property initializer
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'next.IsKind(SyntaxKind.EqualsToken) && owner is AccessorListSyntax'
    Replace = 'false'
    Tests = 'BlankLineRunsTests'
}
