# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '&& isOn(braces.IsElement ? DiagnosticIds.SingleLineElement : DiagnosticIds.SingleLineStatementBlock)'
    Replace = ''
    Tests = 'BracesTests'
}
