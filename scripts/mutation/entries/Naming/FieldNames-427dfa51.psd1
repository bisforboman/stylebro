# COM GUID fields (IID_IContextMenu) are interop names
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '{ "IID_", "CLSID_", "LIBID_", "DIID_", "CATID_", "GUID_" }'
    Replace = '{ "IID#" }'
    Tests = 'FieldNamingTests'
}
