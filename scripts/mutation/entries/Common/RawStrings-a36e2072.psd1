# A fix that changes a raw string literal is put back (trial 66, moq4).
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'Same(Values(oldRoot), Values(newRoot))'
    Replace = 'true'
    Tests = 'RawStringsTests'
}
