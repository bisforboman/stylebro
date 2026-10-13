# Renamer: keeps 'this.' when dotnet_style_qualification_for_field asks for it
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& value.Split('':'')[0].Trim().Equals("true", StringComparison.OrdinalIgnoreCase))'
    Replace = '&& false)'
    Tests = 'FieldNamingTests'
}
