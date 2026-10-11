# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'if (sameLine && items.FirstOrDefault('
    Replace = 'if (false && items.FirstOrDefault('
    Tests = 'ParameterLayoutTests'
}
