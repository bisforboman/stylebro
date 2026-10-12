# BRO1147 stylebro_allow_null_forgiving_on_unannotated: unannotated properties keep their '!' (Sonar's S8969).
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'IPropertySymbol property => property.NullableAnnotation == NullableAnnotation.None,'
    Replace = 'IPropertySymbol property => false,'
    Tests = 'NullForgivingTests'
}
