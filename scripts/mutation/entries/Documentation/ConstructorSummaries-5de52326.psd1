# Trial 43: a type header with '#if' branches makes the cref depend on the build: BRO1606 skips it.
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = '            || HasConditionalHeader(type))'
    Replace = '            )'
    Tests = 'DocumentationTests.ConstructorSummaries_SkipTypesWhoseHeaderHasIfBranches'
}
