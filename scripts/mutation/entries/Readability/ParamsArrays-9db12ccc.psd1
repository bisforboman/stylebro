# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '|| (elements.Count == 1 && model.ClassifyConversion(elements[0], parameter.Type).IsImplicit)'
    Replace = ''
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
