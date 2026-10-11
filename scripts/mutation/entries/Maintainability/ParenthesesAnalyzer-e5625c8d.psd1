# BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/ParenthesesAnalyzer.cs'
    Find = 'if (Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, rule.Id, c.CancellationToken))'
    Replace = 'if (true)'
    Tests = 'PatternParenthesesTests.FadedParentheses_AreNotReported_WhenTheRuleIsOff'
}
