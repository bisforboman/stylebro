# Trial 67 (RestEase): init carries the suppressions of the built-in rules it turns off over to the rules that replace them.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var (addedIde, filesIde, _, _) = Program.RewriteSuppressions(root, Suppressions.WithIde(new Dictionary<string, SortedSet<string>>()), write, blocked: blockedIde);'
    Replace = 'var (addedIde, filesIde, _, _) = (0, 0, 0, 0);'
    Tests = 'Init_CarriesIde0040SuppressionsOver'
}
