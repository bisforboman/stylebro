# Trial 22: a T4 output only when the build runs templates (CommunityToolkit)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return Regex.IsMatch(build, @"TransformOnBuild\s*>\s*true|TextTransform|dotnet[- ]t4", RegexOptions.IgnoreCase);'
    Replace = 'return true;'
    Tests = 'MigrationTests.GeneratedFileNote_LeavesOutVisualStudioToolOutput'
}
