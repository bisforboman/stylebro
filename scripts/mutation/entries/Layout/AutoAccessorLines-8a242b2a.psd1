# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'if (BlankLineRuns.JudgesGapAfter(close, next, text, isOn, gapIsReplaced: false))'
    Replace = 'if (false)'
    Tests = 'AutoAccessorLinesTests'
}
