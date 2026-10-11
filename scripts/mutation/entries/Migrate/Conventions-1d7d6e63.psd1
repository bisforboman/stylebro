# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '.Select(l => l.Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None)).ToList())'
    Replace = '.Select(l => l.ToList())'
    Tests = 'MigrationTests.Conventions_CountTheUsingOrder'
}
