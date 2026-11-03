# A kept file is a Not checked gap (format and check end Not verified, owner, trial 66).
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'return $"Not checked: {('
    Replace = 'return $"Kept: {('
    Tests = 'RawStringsTests'
}
