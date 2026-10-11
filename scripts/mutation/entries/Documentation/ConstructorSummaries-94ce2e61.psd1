# BRO1603 (periods): closing punctuation, entities, excluded tags; BRO1606: a summary starting with <para>
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'GetTextStart(paragraph ?? summary'
    Replace = 'GetTextStart(summary'
    Tests = 'DocumentationTests'
}
