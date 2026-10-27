# BRO1117: an item on a line after its comma keeps its own indentation (FusionCache, trial 56).
@{
    File = 'src/StyleBro.Analyzers/Readability/ListGaps.cs'
    Find = 'if (afterComma.IndexOf(''\n'') >= 0)'
    Replace = 'if (false)'
    Tests = 'ListGapsTests.CommaAloneOnItsLine_ItemKeepsItsIndentation'
}
