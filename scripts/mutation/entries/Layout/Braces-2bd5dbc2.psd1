# BRO1514-BRO1516 (braces)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'statement.ContainsDirectives || parent.ContainsDirectives'
    Replace = 'false'
    Tests = 'BracesTests'
}
