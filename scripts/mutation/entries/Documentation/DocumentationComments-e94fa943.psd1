# BRO1608-BRO1610 skip a malformed comment
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'list.Count == 0 || IsMalformed(documentation)'
    Replace = 'list.Count == 0'
    Tests = 'DocumentationTests.MalformedDocumentation_IsLeftAloneByTheTagRules'
}
