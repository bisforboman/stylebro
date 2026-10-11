# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'
    Find = 'if (!Regex.IsMatch(prefix, '
    Replace = 'if (false && Regex.IsMatch(prefix, '
    Tests = 'DocumentationTests'
}
