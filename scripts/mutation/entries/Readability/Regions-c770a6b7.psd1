# Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
@{
    File = 'src/StyleBro.Analyzers/Readability/Regions.cs'
    Find = 'if (beforeText.Length == 0 && after + 1 < text.Lines.Count'
    Replace = 'if (false && after + 1 < text.Lines.Count'
    Tests = 'FixOrderTests'
}
