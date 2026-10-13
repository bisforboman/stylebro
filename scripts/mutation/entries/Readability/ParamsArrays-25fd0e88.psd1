# BRO1151: a single element that binds as the array itself (default!) must keep its array.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '            && BindsExpanded(bound.Model, bound.Node, list.Arguments.Count - 1, ((IArrayTypeSymbol)parameter.Type).ElementType, cancellationToken)'
    Replace = ''
    Tests = 'FullyQualifiedName~ParamsArrayTests'
}
