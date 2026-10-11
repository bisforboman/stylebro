# BRO1408 (redundant base type)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'
    Find = 'EnumUnderlyingType.SpecialType: SpecialType.System_Int32'
    Replace = 'EnumUnderlyingType: not null'
    Tests = 'RedundantBaseTypeTests'
}
