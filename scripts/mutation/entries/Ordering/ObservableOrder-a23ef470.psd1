# BRO1001: a type whose members code reads by position keeps its order
@{
    File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'
    Find = 'return !Enumerable.Range(0, members.Count).Where(IsRead).SequenceEqual(order.Where(IsRead));'
    Replace = 'return false;'
    Tests = 'MemberOrderingTests.FieldsReadByPositionElsewhere|FullyQualifiedName~KeptFindingsTests.ATypeAProjectThisRunDoesntLoad'
}
