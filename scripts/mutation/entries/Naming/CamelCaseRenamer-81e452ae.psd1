# Trial 26: XAML can't reach private fields
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'return symbol is not IFieldSymbol { DeclaredAccessibility: Accessibility.Private } && names.Files.Xaml.TryGetValue(symbol.Name, out var xaml)'
    Replace = 'return names.Files.Xaml.TryGetValue(symbol.Name, out var xaml)'
    Tests = 'KeptFindingsTests.AnEventHandlerInAXamlFile_IsKept_APrivateFieldIsNot'
}
