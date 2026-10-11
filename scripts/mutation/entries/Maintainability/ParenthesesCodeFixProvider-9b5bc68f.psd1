# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/ParenthesesCodeFixProvider.cs'
    Find = '&& Precedence.IsWanted(DiagnosticIds.ConditionalPrecedence, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree))'
    Replace = ''
    Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenTheSdkSettingSaysNever'
}
