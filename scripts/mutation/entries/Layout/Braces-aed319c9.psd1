# BRO1514-BRO1516 (braces)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'if (statement.DescendantTokens().Any('
    Replace = 'if (false && statement.DescendantTokens().Any('
    Tests = 'BracesTests'
}
