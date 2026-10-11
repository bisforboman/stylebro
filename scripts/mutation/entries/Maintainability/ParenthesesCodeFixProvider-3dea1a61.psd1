# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/ParenthesesCodeFixProvider.cs'
    Find = 'Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.ConditionalPrecedence, cancellationToken)'
    Replace = 'true'
    Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenBro1407IsOff'
}
