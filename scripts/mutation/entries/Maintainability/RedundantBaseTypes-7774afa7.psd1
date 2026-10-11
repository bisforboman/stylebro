# BRO1408 (redundant base type)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'
    Find = 'return Trivia.IsBlank(declaration, span) ?'
    Replace = 'return true ?'
    Tests = 'RedundantBaseTypeTests'
}
