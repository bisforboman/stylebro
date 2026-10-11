# BRO1150 (Where before Count/Any/...)
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = '|| NullChecks.IsInExpressionTree(terminal, model, cancellationToken)'
    Replace = ''
    Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported'
}
