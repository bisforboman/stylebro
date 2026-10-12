# BRO1151: elements without a type of their own (method groups, conditionals) stay in the array.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '        || model.GetTypeInfo(element, cancellationToken).Type is null;'
    Replace = '        ;'
    Tests = 'FullyQualifiedName~ParamsArrayTests'
}
