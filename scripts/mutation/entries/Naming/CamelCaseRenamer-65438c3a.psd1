# BRO1301-BRO1309: a rename that lets IDE0042 report is dropped
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (itemChanges.Count > 0)'
    Replace = 'if (false)'
    Tests = 'CamelCaseNamingTests.RenamesThatLetIde0042'
}
