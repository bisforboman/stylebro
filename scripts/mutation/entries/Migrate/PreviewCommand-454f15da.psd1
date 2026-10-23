# Trial 43: the preview adds ignored .editorconfig files, not build output's.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '!Regex.IsMatch(f, "(^|/)(bin|obj)/") && '
    Replace = ''
    Tests = 'MigrationTests.AddIgnoredSettings'
}
