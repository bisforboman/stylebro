# run from the folder holding the projects
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var root = File.Exists(path) ? WorkingRoot(path) : path;'
    Replace = 'var root = File.Exists(path) ? Path.GetDirectoryName(path)! : path;'
    Tests = 'FullyQualifiedName~MigrationTests.Format_WorksFromTheFolderHoldingASolutionsProjects'
}
