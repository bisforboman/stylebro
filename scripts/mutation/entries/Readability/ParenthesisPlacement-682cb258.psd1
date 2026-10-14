# Options: stylebro_closing_parenthesis_placement (BRO1110), stylebro_split_list_first_item (BRO1107/BRO1108)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = 'if (items.Count > 0 && !close.IsMissing && Line(text, items[items.Count - 1].Span.End) != Line(text, close.SpanStart))'
    Replace = 'if (false)'
    Tests = 'ParenthesisPlacementTests'
}
