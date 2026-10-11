# BRO1142: an initializer that names the type uses csharp_style_var_when_type_is_apparent
@{
    File = 'src/StyleBro.Analyzers/Readability/VarPreference.cs'
    Find = 'value is ObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax'
    Replace = 'value is null'
    Tests = 'CombinedFieldsTests.Locals_'
}
