# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'return paragraph is null ? GetBlankFinding(member, summary, text, standard) : null;'
    Replace = 'return null;'
    Tests = 'DocumentationTests'
}
