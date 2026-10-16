# Trial 24: a '#' starting the first line of '$"""' + line break (the break is the start token's)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'strings.Add(token.SpanStart + at);'
    Replace = 'if (token.Text.LastIndexOf(''\n'', Math.Max(at - 1, 0)) >= 0) strings.Add(token.SpanStart + at);'
    Tests = 'MigrationTests.Format_ProtectsTheFirstLineOfEveryStringShape'
}
