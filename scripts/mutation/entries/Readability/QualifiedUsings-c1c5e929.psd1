# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'
    Find = '|| (!top && (type.IsTupleType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T))'
    Replace = ''
    Tests = 'QualifiedUsingsTests'
}
