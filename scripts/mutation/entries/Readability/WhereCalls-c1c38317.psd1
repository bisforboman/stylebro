# BRO1150: List/array Exists and Find where Sonar S6605/S6602 are on
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = 'return Trivia.IsBlank(terminal, TextSpan.FromBounds(receiver.Span.End, access.Name.SpanStart))'
    Replace = 'return true'
    Tests = 'WhereBeforeTerminalTests.CollectionMethodsThatWouldNotBind'
}
