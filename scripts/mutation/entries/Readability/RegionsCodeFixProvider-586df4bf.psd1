# BRO1112: a standalone comment keeps one blank line below removed regions (trial 27)
@{
    File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'
    Find = 'if (below.Count == 0 && IsOn(DiagnosticIds.MultipleBlankLines))'
    Replace = 'if (false)'
    Tests = 'RegionsTests.EmptyRegionsBelowAComment_TheBlankLinesGoOnce'
}
