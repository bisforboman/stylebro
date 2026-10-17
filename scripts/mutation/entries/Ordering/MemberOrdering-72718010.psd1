# BRO1001: a blank line below a one-line body's '}' belongs to the brace (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '|| (isOn is not null && previous.GetLastToken() is { RawKind: (int)SyntaxKind.CloseBraceToken } brace'
    Replace = '|| (false && previous.GetLastToken() is { RawKind: (int)SyntaxKind.CloseBraceToken } brace'
    Tests = 'FixOrderTests.Sort_ExpandedOneLineType_WithoutBro1505'
}
