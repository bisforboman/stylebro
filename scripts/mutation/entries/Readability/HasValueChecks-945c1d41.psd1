# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = 'if ((negated && node.SpanStart - target.SpanStart != 1)'
    Replace = 'if ((false)'
    Tests = 'HasValueTests'
}
