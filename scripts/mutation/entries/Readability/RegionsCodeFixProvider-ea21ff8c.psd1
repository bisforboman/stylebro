# Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
@{
    File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'
    Find = 'while (line.LineNumber > 0 && string.IsNullOrWhiteSpace(line.ToString()))'
    Replace = 'while (false)'
    Tests = 'RegionsTests'
}
