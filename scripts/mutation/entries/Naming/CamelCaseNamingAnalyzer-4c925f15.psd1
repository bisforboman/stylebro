# Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| CamelCaseNames.IsNameObservable(node.Parent!.Parent!, oldName, context.SemanticModel, context.CancellationToken))'
    Replace = ')'
    Tests = 'ParameterMatchesBaseTests'
}
