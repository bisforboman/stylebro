# Fields of a type with [DllImport] methods are interop names
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'a.AttributeClass?.Name is "DllImportAttribute" or "LibraryImportAttribute"));'
    Replace = 'a.AttributeClass?.Name is "LibraryImportAttribute"));'
    Tests = 'FieldNamingTests'
}
