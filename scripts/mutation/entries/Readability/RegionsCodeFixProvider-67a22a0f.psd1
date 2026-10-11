# Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
@{
    File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'
    Find = 'union[union.Count - 1].Span.End >= span.Start'
    Replace = 'false'
    Tests = 'RegionsTests'
}
