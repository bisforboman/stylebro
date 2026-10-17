# BRO1301: an extension method keeps its 'this.'
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& member is not IMethodSymbol { ReducedFrom: not null }'
    Replace = '&& true'
    Tests = 'CamelCaseNamingTests.AThisBeforeAnExtensionMethod_Stays'
}
