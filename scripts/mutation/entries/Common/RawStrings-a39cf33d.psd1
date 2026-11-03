# Literals that swap places (a member reorder) are not a change.
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'values.Sort(StringComparer.Ordinal);'
    Replace = ''
    Tests = 'RawStringsTests'
}
