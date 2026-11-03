# A kept file outside the folder the command runs in is named by its full path.
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'return $"Not checked: {(relative.StartsWith("..", StringComparison.Ordinal) ? path : relative)}'
    Replace = 'return $"Not checked: {(relative.StartsWith("..", StringComparison.Ordinal) ? relative : path)}'
    Tests = 'RawStringsTests'
}
