# Syntax node actions from a tree action (NodeActions): the root is visited too
@{
    File = 'src/StyleBro.Analyzers/TreeWalk.cs'
    Find = 'new Dictionary<SyntaxKind, List<SyntaxNode>> { [root.Kind()] = new() { root } };'
    Replace = 'new Dictionary<SyntaxKind, List<SyntaxNode>>();'
    Tests = 'NodeActionsTests'
}
