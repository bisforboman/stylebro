# BRO1151: target-typed elements (new(), lambdas, ...) stay in the array.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '|| elements.Any(e => IsTargetTyped(e, model, cancellationToken)))'
    Replace = ')'
    Tests = 'FullyQualifiedName~ParamsArrayTests'
}
