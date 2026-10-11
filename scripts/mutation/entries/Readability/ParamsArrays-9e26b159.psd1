# BRO1151 (params arrays)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'
    Find = '|| (elements.Count > 1 && line != text.Lines.GetLineFromPosition(list.Arguments[0].SpanStart).LineNumber)'
    Replace = ''
    Tests = 'ParamsArrayTests.OtherArrays_AreNotReported'
}
