# BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'
    Find = 'return changes.Count == 0 ? null :'
    Replace = 'return false ? null :'
    Tests = 'DocumentationStyleTests'
}
