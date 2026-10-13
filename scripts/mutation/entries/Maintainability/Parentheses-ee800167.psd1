# Trial 18: BRO1405/BRO1410 keep a comment before the closing parenthesis
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'is { RawKind: not 0 } lastKept ? lastKept.Span.End : inner.Span.End;'
    Replace = 'is { RawKind: not 0 } lastKept ? inner.Span.End : inner.Span.End;'
    Tests = 'ParenthesesTests'
}
