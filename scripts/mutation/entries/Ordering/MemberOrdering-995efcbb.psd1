# BRO1001 stylebro_keep_overloads_together (Sonar S4136)
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = 'if (anchors is not null && anchors[a] != anchors[b])'
    Replace = 'if (false)'
    Tests = 'MemberOrderingTests'
}
