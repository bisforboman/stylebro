# BRO1108: items that already start a line get the moved items' indentation.
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = '        if (!sameLine && lineStarts is not null && openLine.Start + LeadingWhitespace(text, openLine).Length != open.SpanStart)'
    Replace = '        if (false)'
    Tests = 'StyleBro.Tests.ParameterLayoutTests'
}
