# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'Regex.Replace(File.ReadAllText(candidate), "<!--.*?-->", string.Empty, RegexOptions.Singleline)'
    Replace = 'File.ReadAllText(candidate)'
    Tests = 'MigrationTests'
}
