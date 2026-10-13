# Trial 18: BRO1405/BRO1410 keep a comment after the opening parenthesis
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'is { RawKind: not 0 } firstKept ? firstKept.SpanStart : inner.SpanStart;'
    Replace = 'is { RawKind: not 0 } firstKept ? inner.SpanStart : inner.SpanStart;'
    Tests = 'ParenthesesTests'
}
