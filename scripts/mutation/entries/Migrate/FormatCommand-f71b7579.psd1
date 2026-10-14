# only warning or error enforce IDE0005
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'is Microsoft.CodeAnalysis.ReportDiagnostic.Warn or Microsoft.CodeAnalysis.ReportDiagnostic.Error;'
    Replace = 'is not null;'
    Tests = 'FullyQualifiedName~MigrationTests.Format_FixesUnusedUsings'
}
