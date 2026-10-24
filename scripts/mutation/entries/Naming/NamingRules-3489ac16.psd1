# Trial 50: no static method is renamed to Main
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = 'if (newName == "Main" && symbol is IMethodSymbol { IsStatic: true })'
    Replace = 'if (newName == "Main " && symbol is IMethodSymbol { IsStatic: true })'
    Tests = 'PascalCaseNamingTests.AStaticMethodRenamedToMain_IsKept'
}
