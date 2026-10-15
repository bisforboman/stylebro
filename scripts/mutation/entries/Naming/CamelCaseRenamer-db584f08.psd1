# Trial 23 (jint): dynamic uses in the solution
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'AddDynamic(text, $"{document.FilePath ?? document.Name}({line + 1})");'
    Replace = ''
    Tests = 'KeptFindingsTests.AFieldUsedThroughDynamic_IsKept'
}
