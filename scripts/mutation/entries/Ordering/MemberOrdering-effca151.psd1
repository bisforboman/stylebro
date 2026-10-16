# Trial 26: the generated-part signal
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '        && Naming.NamespaceNames.HasGeneratedPart(symbol);'
    Replace = '        && false;'
    Tests = 'MemberOrderingTests.TypesWithAGeneratedPart_KeepTheirDataMembersInOrder'
}
