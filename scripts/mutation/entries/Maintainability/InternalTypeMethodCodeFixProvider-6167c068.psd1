# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = ': names.Nameofs.Contains(name) && await IsInNameofAsync'
    Replace = ': false && await IsInNameofAsync'
    Tests = 'InternalTypeMethodTests'
}
