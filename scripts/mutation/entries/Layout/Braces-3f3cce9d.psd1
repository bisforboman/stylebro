@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'return LeadingWhitespace(text, line).Length == brace.SpanStart - line.Start ? line : null;'
    Replace = 'return line;'
    Tests = 'BracesTests.SameLineStatement_BodyIndentedByTheFormatter'
}
