# BRO1603 skips malformed doc XML
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 'if (malformed.Value)'
    Replace = 'if (false)'
    Tests = 'FullyQualifiedName~DocumentationTests.MalformedDocumentation_IsLeftAlone'
}
