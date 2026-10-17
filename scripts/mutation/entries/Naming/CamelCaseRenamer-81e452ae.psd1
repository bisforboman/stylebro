# Trial 26: XAML can't reach private fields
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (symbol is IFieldSymbol { DeclaredAccessibility: Accessibility.Private })'
    Replace = 'if (false)'
    Tests = 'KeptFindingsTests.AnEventHandlerInAXamlFile_IsKept_APrivateFieldIsNot'
}
