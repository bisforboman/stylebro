# A lone surrogate is escaped in the repository-names file (Orleans: UTF-8 could not hold it)
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = '            else if (char.IsSurrogate(c))'
    Replace = '            else if (char.IsSurrogate(c) && i < 0)'
    Tests = 'KeptFindingsTests'
}
