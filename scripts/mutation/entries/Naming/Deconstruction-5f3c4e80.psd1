# csharp_style_deconstructed_variable_declaration = false turns IDE0042 off
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = 'if (style?.Trim().StartsWith("false", System.StringComparison.OrdinalIgnoreCase) == true)'
    Replace = 'if (false)'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
