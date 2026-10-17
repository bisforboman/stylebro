# Trial 27: a self-closing .slnx project has no Build element
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '""[^>]*(?<!/)>(?<body>'
    Replace = '""[^>]*>(?<body>'
    Tests = 'MigrationTests.NotBuiltFolders_ReadASlnxsBuildFalse'
}
