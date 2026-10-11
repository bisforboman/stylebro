# "Field" must be a word of the name
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = '            if (end == name.Length || char.IsUpper(name[end]))'
    Replace = '            if (true)'
    Tests = 'KeptFindingsTests'
}
