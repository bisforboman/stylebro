# An interpolation hole is one placeholder: reformatting its expression is not a change.
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = '.Insert(hole.Span.Start - interpolated.SpanStart, "{}")'
    Replace = '.Insert(hole.Span.Start - interpolated.SpanStart, hole.ToString())'
    Tests = 'RawStringsTests'
}
