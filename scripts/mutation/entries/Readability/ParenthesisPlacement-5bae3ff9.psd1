# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = 'GetCloseFix(list, text, ownLine, ownLine && isOn'
    Replace = 'GetCloseFix(list, text, false, false && isOn'
    Tests = 'FixOrderTests.SameLineJoins'
}
