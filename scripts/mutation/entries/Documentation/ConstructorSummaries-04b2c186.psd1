# Trial 43: an '#if' above the whole type doesn't make BRO1606 skip it.
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '|| (token != type.GetFirstToken() && ('
    Replace = '|| (true && ('
    Tests = 'DocumentationTests.ConstructorSummaries_SkipTypesWhoseHeaderHasIfBranches'
}
