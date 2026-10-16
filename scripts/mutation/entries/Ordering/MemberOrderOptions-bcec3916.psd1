# BRO1001 fields_first: fields keep their order
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'readonlyFirst: false,'
    Replace = 'readonlyFirst: true,'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
