# Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
@{
    File = 'src/StyleBro.Analyzers/Naming/DisabledCode.cs'
    Find = '&& declaration.FullSpan.Contains(trivia.Span)'
    Replace = ''
    Tests = 'PrefixNamingTests.DisabledCodeWithoutTheName_DoesNotBlock'
}
