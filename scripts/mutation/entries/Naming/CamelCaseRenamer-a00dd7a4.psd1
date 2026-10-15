# Trial 23: a snake_case property keeps the type's other properties too
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '.Any(p => p.Name.TrimStart(''_'').IndexOf(''_'') >= 0'
    Replace = '.Any(p => false'
    Tests = 'KeptFindingsTests.APropertyNextToASnakeCaseOne_IsKept'
}
