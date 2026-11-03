# The closing quotes' indentation is not part of the text: a literal that moves as a whole is no change.
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'i > 0 && indent.Length > 0 && line.StartsWith(indent, StringComparison.Ordinal) ? line.Substring(indent.Length) : line'
    Replace = 'line'
    Tests = 'RawStringsTests'
}
