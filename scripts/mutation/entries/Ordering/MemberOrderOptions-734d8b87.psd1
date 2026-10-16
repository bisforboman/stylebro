# BRO1001 fields_first: accessibility isn't ordered
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'new List<int[]> { AllBut(AccessCount, -1) },'
    Replace = 'null,'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
