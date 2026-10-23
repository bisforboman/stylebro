# trial 49: a parameter with an attribute keeps its name (frameworks bind it)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (symbols.OfType<IParameterSymbol>().Any(HasBindingAttribute))'
    Replace = 'if (false)'
    Tests = 'KeptFindingsTests.AParameterWithAnAttribute_IsKept'
}
