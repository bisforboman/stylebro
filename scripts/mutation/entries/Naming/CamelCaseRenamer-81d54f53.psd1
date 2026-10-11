# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
# Generated code and C# versions (real-world: eShopOnWeb's Razor pages, Mapperly's generated accessors, LibGit2Sharp's net472)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| NamespaceNames.IsGenerated(tree))'
    Replace = '|| false)'
    Tests = 'FieldNamingTests'
}
