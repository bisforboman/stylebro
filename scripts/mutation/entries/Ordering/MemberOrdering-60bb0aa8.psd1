# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (EndsWithDirective(layout) && StartsWithDocComment(split[source].Content) && !StartsWithDocComment(split[slot].Content))'
    Replace = 'if (false)'
    Tests = 'MemberOrderingTests'
}
