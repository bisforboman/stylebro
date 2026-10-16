# Trial 26: a type with a generated part keeps its data order
@{
    File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'
    Find = 'var serialized = generatedPart || '
    Replace = 'var serialized = '
    Tests = 'MemberOrderingTests.TypesWithAGeneratedPart_KeepTheirDataMembersInOrder'
}
