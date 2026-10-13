# BRO1606: spaced cref attribute
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'Regex.Replace(m.Value, @"\s*=\s*", "=")'
    Replace = 'm.Value'
    Tests = 'FullyQualifiedName~ConstructorSentence_WithSpacedCref_IsAccepted'
}
