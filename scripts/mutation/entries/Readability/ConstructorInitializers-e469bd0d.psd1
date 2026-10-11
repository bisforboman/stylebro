# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'
    Find = '|| !IsPlain(close.TrailingTrivia, '
    Replace = '|| false && !IsPlain(close.TrailingTrivia, '
    Tests = 'ConstructorInitializerLineTests'
}
