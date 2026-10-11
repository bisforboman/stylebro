# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/PrecedenceAnalyzer.cs'
    Find = 'if (!Precedence.IsWanted(id, options))'
    Replace = 'if (false)'
    Tests = 'PrecedenceTests'
}
