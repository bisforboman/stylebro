# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = '&& (field.DeclaredAccessibility == Accessibility.Private || !IsInDisabledCode(field, context))'
    Replace = ''
    Tests = 'FieldNamingTests.NonPrivateFieldsInDisabledCode_AreSkipped'
}
