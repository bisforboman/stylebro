# Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
@{
    File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'
    Find = 'if (IsOn(DiagnosticIds.BlankLineAfterComment))'
    Replace = 'if (true)'
    Tests = 'RegionsTests'
}
