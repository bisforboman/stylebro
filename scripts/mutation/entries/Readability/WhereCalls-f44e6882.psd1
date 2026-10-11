# BRO1150 (Where before Count/Any/...)
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = 'if (GetLinqMethod(model.GetSymbolInfo(where, cancellationToken).Symbol) is null'
    Replace = 'if (false'
    Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported'
}
