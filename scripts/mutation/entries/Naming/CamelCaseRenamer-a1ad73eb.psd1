# Renamer: 'm_x' only a field has
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'name[1] == ''_'' && name[0] is ''m'' or ''s'' or ''t'''
    Replace = 'false'
    Tests = 'KeptFindingsTests.AFieldNameOnlyAFieldHas'
}
