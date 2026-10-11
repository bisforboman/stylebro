# BRO1404/BRO1007 (access modifiers)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'
    Find = 'if (!preference.Required'
    Replace = 'if (false'
    Tests = 'AccessModifiersTests'
}
