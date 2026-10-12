# BRO1147 allow_null_forgiving_on_unannotated: unannotated method returns.
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'IMethodSymbol method => method.ReturnNullableAnnotation == NullableAnnotation.None,'
    Replace = 'IMethodSymbol method => false,'
    Tests = 'NullForgivingTests'
}
