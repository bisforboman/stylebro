# Trial 9, tool (2026-10-12): edited files keep their line endings
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'File.WriteAllText(file, KeepLineEndings(text ?? FolderLineEnding(file), edited)'
    Replace = 'File.WriteAllText(file, edited'
    Tests = 'MigrationTests.EditedFiles_KeepTheirLineEndings'
}
