# BRO1001 fields_first: every kind but fields shares one place
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderOptions.cs'
    Find = 'new List<int[]> { new[] { (int)MemberKind.Field }, AllBut(KindCount, (int)MemberKind.Field) },'
    Replace = 'null,'
    Tests = 'MemberOrderingTests.FieldsFirst'
}
