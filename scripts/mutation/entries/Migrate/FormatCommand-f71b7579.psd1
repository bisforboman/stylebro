# only warning or error enforce IDE0005
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'UnusedUsings) is Microsoft.CodeAnalysis.ReportDiagnostic.Warn or Microsoft.CodeAnalysis.ReportDiagnostic.Error);'
    Replace = 'UnusedUsings) is not null);'
    Tests = 'MigrationTests.Format_FixesUnusedUsings'
}
