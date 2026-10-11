# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| (keepObservableNames && current is IParameterSymbol && await IsObservableAsync(current).ConfigureAwait(false))'
    Replace = ''
    Tests = 'ParameterMatchesBaseTests'
}
