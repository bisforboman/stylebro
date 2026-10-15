# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '.SelectMany(l => SplitAtDirectives(l.Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None))))'
    Replace = '.SelectMany(l => SplitAtDirectives(l))'
    Tests = 'MigrationTests.Conventions_CountTheUsingOrder'
}
