# Fix order: BRO1001 makes BRO1509's expansion and BRO1505's blank lines before sorting; a one-line type stays on one line
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (slot < count - 1 && !oneLine && '
    Replace = 'if (slot < count - 1 && '
    Tests = 'MemberOrderingTests'
}
