# BRO1001 fields_first: fields keep their order
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'constantsFirst: false,'
    Replace = 'constantsFirst: true,'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
