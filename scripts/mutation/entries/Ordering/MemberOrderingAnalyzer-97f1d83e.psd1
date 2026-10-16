# Trial 26: the analyzer asks for the generated part
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrderingAnalyzer.cs'
    Find = '            MemberOrdering.HasGeneratedPart(container, context.SemanticModel, context.CancellationToken));'
    Replace = '            false);'
    Tests = 'MemberOrderingTests.TypesWithAGeneratedPart_KeepTheirDataMembersInOrder'
}
