# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'array is CollectionExpressionSyntax ? info.ConvertedType : info.Type'
    Replace = 'info.ConvertedType'
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
