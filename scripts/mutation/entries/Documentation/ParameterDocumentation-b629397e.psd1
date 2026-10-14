# BRO1611-BRO1614 skip a malformed comment
@{
    File = 'src/StyleBro.Analyzers/Documentation/ParameterDocumentation.cs'
    Find = 'problems.Count == 0 || documentation.Any(DocumentationComments.IsMalformed)'
    Replace = 'problems.Count == 0'
    Tests = 'DocumentationTests.MalformedDocumentation_IsLeftAloneByTheTagRules'
}
