# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'return SymbolEqualityComparer.Default.Equals(Speculation.SymbolAfterReplacing(model, call, expanded, cancellationToken), method) ? changes : null;'
    Replace = 'return changes;'
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
