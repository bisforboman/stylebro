# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = 'if (removed.Any(span => !Trivia.IsBlank(call, span))'
    Replace = 'if (false'
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
