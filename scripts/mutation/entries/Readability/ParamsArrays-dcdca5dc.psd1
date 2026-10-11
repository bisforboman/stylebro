# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'list.Arguments.Count != method.Parameters.Length'
    Replace = 'false'
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
