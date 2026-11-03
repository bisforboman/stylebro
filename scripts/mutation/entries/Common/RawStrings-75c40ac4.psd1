# The cheap test must not skip documents with a raw string (trial 66).
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'if (!Contains(oldText))'
    Replace = 'if (false)'
    Tests = 'RawStringsTests'
}
