# Public fields of a private type are guarded like private ones (sourcegit TemplateEngine)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '&& !IsInPrivateType(field)'
    Replace = ''
    Tests = 'KeptFindingsTests'
}
