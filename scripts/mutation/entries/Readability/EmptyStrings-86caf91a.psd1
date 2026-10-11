# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = '.Any(t => access.Span.Contains(t.Span)'
    Replace = '.Any(t => false'
    Tests = 'EmptyStringTests'
}
