# BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'brace.RawKind != 0 && isOn(DiagnosticIds.ElementsSeparatedByBlankLine)'
    Replace = 'false'
    Tests = 'BlankLineRunsTests'
}
