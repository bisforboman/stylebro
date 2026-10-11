# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'StateWithout(node, model, cancellationToken) == NullableFlowState.NotNull'
    Replace = 'info.Nullability.FlowState == NullableFlowState.NotNull'
    Tests = 'NullForgivingTests'
}
