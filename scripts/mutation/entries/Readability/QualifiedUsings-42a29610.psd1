# Trial 21: type arguments keep their spelling
@{
    File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'
    Find = ': syntax.DescendantNodesAndSelf().OfType<PredefinedTypeSyntax>().Any()'
    Replace = ': true'
    Tests = 'QualifiedUsingsTests.BuiltInTypeArguments_KeepTheirSpelling'
}
