# BRO1303/BRO1306/BRO1307 (fields)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '&& field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;'
    Replace = '&& field.DeclaredAccessibility != Accessibility.Private;'
    Tests = 'FieldNamingTests'
}
