# Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (!projectFound && Directory.GetFiles(current, "*.csproj")'
    Replace = 'if (Directory.GetFiles(current, "*.csproj")'
    Tests = 'MigrationTests.Init_CountsBro1601OnlyWhereDocumentationIsGenerated'
}
