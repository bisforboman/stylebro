# BRO1149 wraps && at the end when BRO1520 is off (sourcegit)
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = 'isOn(DiagnosticIds.OperatorPlacement) && '
    Replace = ''
    Tests = 'NestedIfTests'
}
