# BRO1001 at the namespace level
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (container is CompilationUnitSyntax && members[0].GetFirstToken().GetPreviousToken().IsKind(SyntaxKind.None)'
    Replace = 'if (false'
    Tests = 'MemberOrderingTests'
}
