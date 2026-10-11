# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/DisabledCode.cs'
    Find = ' && (end == text.Length || !IsIdentifierChar(text[end]))'
    Replace = ''
    Tests = 'PrefixNamingTests.DisabledCodeWithoutTheName_DoesNotBlock'
}
