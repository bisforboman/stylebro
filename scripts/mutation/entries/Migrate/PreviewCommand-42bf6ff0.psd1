# Trial 25: never a copy outside the temporary folder
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (to.StartsWith(top, StringComparison.OrdinalIgnoreCase) && File.Exists(file))'
    Replace = 'if (File.Exists(file))'
    Tests = 'MigrationTests.Preview_CopiesTheFilesProjectsCompileFromOutside_ToTheSameRelativePlace'
}
