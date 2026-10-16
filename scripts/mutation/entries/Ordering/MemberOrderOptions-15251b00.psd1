# BRO1001 same_access: only overloads with the same access are grouped
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'keepOverloadsTogether, sameAccess, orderNamespaces);'
    Replace = 'keepOverloadsTogether, false, orderNamespaces);'
    Tests = 'MemberOrderingTests.Overloads_SameAccess'
}
