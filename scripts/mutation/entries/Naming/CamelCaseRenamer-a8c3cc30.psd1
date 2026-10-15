# Trial 23 (jint): a member used through dynamic keeps its name
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (names.Dynamic.TryGetValue(symbol.Name, out var dynamicUse))'
    Replace = 'if (false && names.Dynamic.TryGetValue(symbol.Name, out var dynamicUse))'
    Tests = 'KeptFindingsTests.AFieldUsedThroughDynamic_IsKept'
}
