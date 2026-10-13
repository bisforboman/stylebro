# a tuple used as a whole is not reported by IDE0042
@{
    File = 'src/StyleBro.CodeFixes/Naming/Deconstruction.cs'
    Find = 'if (name.Parent is not MemberAccessExpressionSyntax access)
            {
                return false;'
    Replace = 'if (name.Parent is not MemberAccessExpressionSyntax access)
            {
                continue;'
    Tests = 'TupleElementNamingTests.RenamesThatLetIde0042'
}
