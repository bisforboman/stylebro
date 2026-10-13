# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"""csharpier""", System.Text.RegularExpressions.RegexOptions.IgnoreCase)'
    Replace = ''
    Tests = 'MigrationTests.CSharpier_IsDetected'
}
