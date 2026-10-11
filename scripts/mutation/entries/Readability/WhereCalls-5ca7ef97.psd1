# BRO1150 (Where before Count/Any/...)
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = '|| !Trivia.IsBlank(terminal, TextSpan.FromBounds(argument.Span.End, terminal.Span.End))'
    Replace = ''
    Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported'
}
