# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'
    Find = 'if (!IsPlain(last.TrailingTrivia, close.LeadingTrivia))'
    Replace = 'if (false)'
    Tests = 'ConstructorInitializerLineTests'
}
