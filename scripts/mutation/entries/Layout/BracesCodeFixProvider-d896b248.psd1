# BRO1514-BRO1516 (braces)
@{
    File = 'src/StyleBro.CodeFixes/Layout/BracesCodeFixProvider.cs'
    Find = '.Where(c => Braces.GetChanges(new[] { c }, text, options) is not null)'
    Replace = ''
    Tests = 'BracesTests'
}
