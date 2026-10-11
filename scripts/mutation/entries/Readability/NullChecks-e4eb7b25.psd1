# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '&& op.Parameters[0].Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T'
    Replace = ''
    Tests = 'NullCheckTests'
}
