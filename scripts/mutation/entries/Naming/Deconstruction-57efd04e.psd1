# IDE0042 reports foreach variables too
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = '            else if (node is ForEachStatementSyntax forEach'
    Replace = '            else if (false && node is ForEachStatementSyntax forEach'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
