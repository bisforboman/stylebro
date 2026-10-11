# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/ParenthesesAnalyzer.cs'
    Find = '&& Precedence.IsWanted(DiagnosticIds.ConditionalPrecedence, c.Options.AnalyzerConfigOptionsProvider.GetOptions(tree))'
    Replace = ''
    Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenTheSdkSettingSaysNever'
}
