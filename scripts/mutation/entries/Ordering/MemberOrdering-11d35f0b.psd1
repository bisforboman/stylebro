# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (kind is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia'
    Replace = 'if (false'
    Tests = 'MemberOrderingTests'
}
