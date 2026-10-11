# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/ParenthesesAnalyzer.cs'
    Find = 'Severities.IsOn(c.Compilation.Options, tree, DiagnosticIds.ConditionalPrecedence, c.CancellationToken)'
    Replace = 'true'
    Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenBro1407IsOff'
}
