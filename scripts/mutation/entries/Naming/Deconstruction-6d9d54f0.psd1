# a tuple used as a whole is not reported by IDE0042
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = '&& (name.Parent is not MemberAccessExpressionSyntax access'
    Replace = '&& (false && name.Parent is not MemberAccessExpressionSyntax access'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
