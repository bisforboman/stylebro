# init: a summary that starts with an element isn't counted for BRO1604/BRO1606 (#132)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = ' && firstToken.IsKind(SyntaxKind.XmlTextLiteralToken)'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountPropertyAndConstructorSummaries_TheStyleCopWayOrTheirOwn'
}
