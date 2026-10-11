# BRO1603 (periods): closing punctuation, entities, excluded tags; BRO1606: a summary starting with <para>
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = '&& !excluded.Contains(name) && '
    Replace = '&& '
    Tests = 'DocumentationTests'
}
