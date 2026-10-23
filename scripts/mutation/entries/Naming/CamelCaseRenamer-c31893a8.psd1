# trial 49: caller-info attributes don't bind a parameter's name
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '"System.Runtime.CompilerServices" or "System.Diagnostics.CodeAnalysis"'
    Replace = '"System.Diagnostics.CodeAnalysis"'
    Tests = 'KeptFindingsTests.AParameterWithAnAttribute_IsKept'
}
