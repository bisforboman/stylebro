# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = 'is not IPropertySymbol { ContainingType.OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })'
    Replace = 'is not IPropertySymbol)'
    Tests = 'HasValueTests'
}
