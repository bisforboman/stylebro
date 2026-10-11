# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = ': names.Nameofs.Contains(name) ? KeptReason.NameInNameof'
    Replace = ': false ? KeptReason.NameInNameof'
    Tests = 'InternalTypeMethodTests'
}
