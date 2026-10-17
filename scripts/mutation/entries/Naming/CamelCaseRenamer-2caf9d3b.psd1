# Renamer: an ordinary word ('options') in a string doesn't keep a private field
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'name.Length > 1 && (name[0] == ''_'' ||'
    Replace = 'name.Length > 1 && (true ||'
    Tests = 'KeptFindingsTests.AFieldWhoseNameIsAnOrdinaryStringElsewhere'
}
