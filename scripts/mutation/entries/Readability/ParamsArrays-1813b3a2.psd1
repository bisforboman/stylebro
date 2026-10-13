# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '            && SymbolEqualityComparer.Default.Equals(bound.Model.GetSymbolInfo(bound.Node, cancellationToken).Symbol, method)'
    Replace = ''
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
