# The string guard matches a nested type name after + (trial 35, Castle Core)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '@"($|[,`\[+])|\+" + escaped + @"(?!\w)"'
    Replace = '@"($|[,`\[+])"'
    Tests = 'PrefixNamingTests.NestedTypeNames'
}
