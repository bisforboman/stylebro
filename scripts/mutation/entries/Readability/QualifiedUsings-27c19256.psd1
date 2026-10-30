# BRO1126 keeps the ? of a nullable reference type argument (trial 63, NLog.Extensions.Logging)
@{
    File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'
    Find = 'if (syntax is NullableTypeSyntax nullable && symbol is ITypeSymbol { IsValueType: false })'
    Replace = 'if (false && syntax is NullableTypeSyntax nullable && symbol is ITypeSymbol { IsValueType: false })'
    Tests = 'StyleBro.Tests.QualifiedUsingsTests.NullableReferenceTypeArguments'
}
