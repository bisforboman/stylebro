# BRO1310 (Hungarian prefixes)
@{
    File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'
    Find = '|| allowed.Contains(match.Groups["prefix"].Value)'
    Replace = ''
    Tests = 'HungarianNamingTests'
}
