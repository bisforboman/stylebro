# BRO1107/BRO1108 indent from the name's line when BRO1109 moves a '(' that starts its line (trial 61)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'if (openMoves && ParenthesisPlacement.GetMisplacedOpen(list, text) is not null)'
    Replace = 'if (false)'
    Tests = 'ParameterLayoutTests.OpenParenthesisOnItsOwnLine_IndentsFromTheLineBRO1109MovesItTo'
}
