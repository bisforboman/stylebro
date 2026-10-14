# Options: stylebro_closing_parenthesis_placement (BRO1110), stylebro_split_list_first_item (BRO1107/BRO1108)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = ' || !IsPlainGap(last, close))'
    Replace = ')'
    Tests = 'ParenthesisPlacementTests'
}
