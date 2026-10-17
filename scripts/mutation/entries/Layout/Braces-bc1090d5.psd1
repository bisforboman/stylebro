# BRO1515 csharpier: only one string is unbreakable (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '} is LiteralExpressionSyntax or InterpolatedStringExpressionSyntax;'
    Replace = '} is not null;'
    Tests = 'BracesTests.WithCSharpier_ABreakableLongReturnIsMultiLine'
}
