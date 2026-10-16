# BRO1001 fields_first: overloads aren't grouped
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'keepOverloadsTogether: false,'
    Replace = 'keepOverloadsTogether: true,'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
