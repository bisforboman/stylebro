# A lone surrogate in a kept finding is written as U+FFFD
@{
    File = 'src/StyleBro.Analyzers/KeptReason.cs'
    Find = '            else if (char.IsSurrogate(chars[i]))'
    Replace = '            else if (char.IsSurrogate(chars[i]) && i < 0)'
    Tests = 'KeptFindingsTests'
}
