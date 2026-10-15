# Trial 22: a Visual Studio custom tool's output isn't the build's
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| Regex.IsMatch(build, $@"<LastGenOutput>\s*{Regex.Escape(name)}\s*<", RegexOptions.IgnoreCase))'
    Replace = '|| false)'
    Tests = 'MigrationTests.GeneratedFileNote_LeavesOutVisualStudioToolOutput'
}
