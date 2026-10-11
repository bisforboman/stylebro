# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '|| (type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T))'
    Replace = ')'
    Tests = 'NullCheckTests'
}
