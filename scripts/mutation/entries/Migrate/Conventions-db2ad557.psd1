# Trial 62: tab-indented lines count.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (line.Span.Length > 0 && text[line.Start] == ''\t'')'
    Replace = 'if (false)'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountFilesIndentedWithTabsOrSpaces'
}
