# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'text ?? FolderLineEnding(file)'
    Replace = 'text'
    Tests = 'MigrationTests.NewFiles_GetTheLineEndings'
}
