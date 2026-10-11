# BRO1150 (Where before Count/Any/...)
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = 'if (GetLinqMethod(Speculation.SymbolAfterReplacing(model, terminal, call, cancellationToken)) is null)'
    Replace = 'if (false)'
    Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported'
}
