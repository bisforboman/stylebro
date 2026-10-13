# BRO1617-BRO1619 skip malformed doc XML
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = 'if (!findings.MoveNext() || DocumentationComments.IsMalformed(documentation))'
    Replace = 'if (!findings.MoveNext())'
    Tests = 'DocumentationStyleTests.MalformedDocumentation_IsLeftAlone'
}
