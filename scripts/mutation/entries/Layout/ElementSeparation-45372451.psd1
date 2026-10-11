# BRO1505: a field below a field that spans several lines; BRO1001's sort and BRO1114's split add that blank line
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = 'member.AttributeLists.Count > 0 ? member.AttributeLists.Last().FullSpan.End : member.SpanStart'
    Replace = 'member.SpanStart'
    Tests = 'ElementSeparationTests'
}
