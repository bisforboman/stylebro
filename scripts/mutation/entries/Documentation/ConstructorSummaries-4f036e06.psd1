# BRO1606 skips malformed doc XML
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'if (documentation is null || DocumentationComments.IsMalformed(documentation))'
    Replace = 'if (documentation is null)'
    Tests = 'FullyQualifiedName~DocumentationTests.MalformedDocumentation_IsLeftAlone'
}
