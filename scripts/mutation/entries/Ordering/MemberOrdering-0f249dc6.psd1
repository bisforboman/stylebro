# BRO1505: a field below a field that spans several lines; BRO1001's sort and BRO1114's split add that blank line
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'return Layout.ElementSeparation.NeedsBlankLine(previous, current, previous.SyntaxTree.GetText(), autoAccessorLines, allowAdjacentSingleLine);'
    Replace = 'return !(previous is FieldDeclarationSyntax && current is FieldDeclarationSyntax);'
    Tests = 'FixOrderTests'
}
