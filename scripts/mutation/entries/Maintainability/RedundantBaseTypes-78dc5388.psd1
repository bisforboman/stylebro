# BRO1408 (redundant base type)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'
    Find = '.Type?.SpecialType == SpecialType.System_Object'
    Replace = '.Type is not null'
    Tests = 'RedundantBaseTypeTests'
}
