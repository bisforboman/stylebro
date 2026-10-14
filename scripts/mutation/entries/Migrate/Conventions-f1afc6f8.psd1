# else/catch/finally after a one-line block don't count (trial 20)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& Line(closed.GetFirstToken().SpanStart) != Line(closeBrace.SpanStart))'
    Replace = ')'
    Tests = 'MigrationTests.Conventions_ElseAfterAOneLineBlock'
}
