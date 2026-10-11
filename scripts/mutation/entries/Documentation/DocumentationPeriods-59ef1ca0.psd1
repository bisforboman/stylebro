# BRO1603 (periods): closing punctuation, entities, excluded tags; BRO1606: a summary starting with <para>
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'
    Find = 't.Kind() is SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlEntityLiteralToken'
    Replace = 't.IsKind(SyntaxKind.XmlTextLiteralToken)'
    Tests = 'DocumentationTests'
}
