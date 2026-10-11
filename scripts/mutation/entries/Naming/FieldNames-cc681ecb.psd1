# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'several && IsAllUpper(first) ? first.ToLowerInvariant() : '
    Replace = ''
    Tests = 'CamelCaseNamingTests.NewName'
}
