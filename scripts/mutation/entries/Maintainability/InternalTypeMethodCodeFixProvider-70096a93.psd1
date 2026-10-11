# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = 'return names.Strings.Contains(name) ? KeptReason.NameInString'
    Replace = 'return false ? KeptReason.NameInString'
    Tests = 'InternalTypeMethodTests'
}
