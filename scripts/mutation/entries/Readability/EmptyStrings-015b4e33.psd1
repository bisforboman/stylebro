# BRO1106 literal style
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = 'is IFieldSymbol { ContainingType.SpecialType: SpecialType.System_String }'
    Replace = 'is IFieldSymbol'
    Tests = 'EmptyStringTests'
}
