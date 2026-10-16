# BRO1508 off for one-line blocks keeps IDE0055 from splitting them
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = ': oneLine.Contains($"dotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.SingleLineStatementBlock}.severity = none") ? BlocksComment'
    Replace = ': false ? BlocksComment'
    Tests = 'MigrationTests.Init_OneLineBlocks_StayOnOneLine'
}
