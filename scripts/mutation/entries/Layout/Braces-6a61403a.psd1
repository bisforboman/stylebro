# BRO1515 csharpier: an unbreakable statement stays on one line (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '&& !IsUnbreakable(child);'
    Replace = ';'
    Tests = 'BracesTests.WithCSharpier_AnUnbreakableLongStatementIsNotMultiLine'
}
