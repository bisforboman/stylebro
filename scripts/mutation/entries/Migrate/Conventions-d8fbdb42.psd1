# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'token.Text.TrimEnd(''l'', ''L'') is'
    Replace = 'token.Text is'
    Tests = 'MigrationTests.Conventions_CountUnsignedSuffixes'
}
