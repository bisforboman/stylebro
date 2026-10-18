# Trial 31: BRO1123 writes BRO1122s suffix (Ursa.Avalonia)
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = 'items[i] = LiteralSuffixes.GetSuffixedLiteral((LiteralExpressionSyntax)argument.Expression, type.ConvertedType, model, cancellationToken)
                    ??'
    Replace = 'items[i] ='
    Tests = 'TupleSyntaxTests.ConvertedLiterals'
}
