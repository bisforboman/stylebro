# BRO1147 allow_null_forgiving_on_unannotated: unannotated fields.
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'IFieldSymbol field => field.NullableAnnotation == NullableAnnotation.None,'
    Replace = 'IFieldSymbol field => false,'
    Tests = 'NullForgivingTests'
}
