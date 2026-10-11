# BRO1409: a nameof of another member with the same name does not keep the method public
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = '    private static async Task<bool> IsInNameofAsync(ISymbol? method, Solution solution, CancellationToken cancellationToken)
    {
        if (method is null)
        {
            return true;
        }
'
    Replace = '    private static async Task<bool> IsInNameofAsync(ISymbol? method, Solution solution, CancellationToken cancellationToken)
    {
        if (method is not null)
        {
            return true;
        }
'
    Tests = 'InternalTypeMethodTests.NameofOfAnotherMemberWithTheName'
}
