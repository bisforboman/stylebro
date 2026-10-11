# SCREAMING_CASE means no lower-case letter: Max_Count is still renamed
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '            && name.IndexOf(''_'') >= 0 && name.Any(char.IsLetter) && !name.Any(char.IsLower))'
    Replace = '            && name.IndexOf(''_'') >= 0 && name.Any(char.IsLetter))'
    Tests = 'FieldNamingTests'
}
