# Trial 58 (ExpressionTreeToString): CS1685 doesn't stop the analyzers
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '{ "CS8002", "CS1685" }'
    Replace = '{ "CS8002" }'
    Tests = 'MigrationTests.BuildCheck_ATypeDefinedInTwoReferences_DoesntStopTheAnalyzers'
}
