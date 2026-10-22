# Performance: BRO1611-BRO1614 look for a tag of the kind before reading anything else
@{
    File = 'src/StyleBro.Analyzers/Documentation/ParameterDocumentation.cs'
    Find = 'tag.StartTag.Name.LocalName.ValueText == element'
    Replace = 'tag.StartTag.Name.LocalName.ValueText == "param"'
    Tests = 'DocumentationTests.TypeParamTags_MatchTheTypeParameters'
}
