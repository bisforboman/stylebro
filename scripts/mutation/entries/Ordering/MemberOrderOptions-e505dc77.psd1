# BRO1001 fields_first: a namespace's types aren't ordered
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'orderNamespaces: false);'
    Replace = 'orderNamespaces: true);'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
