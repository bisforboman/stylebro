# BRO1001 fields_first: fields keep their order
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'staticFirst: false,'
    Replace = 'staticFirst: true,'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
