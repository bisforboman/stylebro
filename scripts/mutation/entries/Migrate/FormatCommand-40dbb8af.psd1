# missing path says Not found
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!File.Exists(path) && !Directory.Exists(path))'
    Replace = 'if (false)'
    Tests = 'FullyQualifiedName~MigrationTests.Format_WorksFromTheFolderHoldingASolutionsProjects'
}
