# BRO1001 stylebro_keep_overloads_together (Sonar S4136)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '.ThenBy(i => anchors[i]).'
    Replace = '.'
    Tests = 'MemberOrderingTests'
}
