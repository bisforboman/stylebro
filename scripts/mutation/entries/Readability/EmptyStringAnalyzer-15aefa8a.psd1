# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStringAnalyzer.cs'
    Find = '!EmptyStrings.PrefersLiteral(c.Options.AnalyzerConfigOptionsProvider.GetOptions(literal.SyntaxTree))'
    Replace = 'true'
    Tests = 'EmptyStringTests'
}
