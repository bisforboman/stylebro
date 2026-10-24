# Trial 50: an instance method may become Main
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = 'if (newName == "Main" && symbol is IMethodSymbol { IsStatic: true })'
    Replace = 'if (newName == "Main" && symbol is IMethodSymbol)'
    Tests = 'PascalCaseNamingTests.AStaticMethodRenamedToMain_IsKept'
}
