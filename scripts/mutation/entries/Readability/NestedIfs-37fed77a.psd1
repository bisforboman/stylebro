# BRO1149 wraps && at the end when BRO1520 is off (sourcegit)
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'if (isOn(DiagnosticIds.OperatorPlacement))'
    Replace = 'if (true)'
    Tests = 'NestedIfTests'
}
