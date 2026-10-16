# stylebro_internal_field_naming = like_private (Microsoft style G2)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '|| (style.InternalLikePrivate && field.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal);'
    Replace = ';'
    Tests = 'FieldNamingTests.InternalLikePrivate'
}
