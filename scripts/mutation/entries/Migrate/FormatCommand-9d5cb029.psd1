# Trial 25: a .g.cs only when a build step names it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return (name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || File.ReadLines(file)'
    Replace = 'return name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || (File.ReadLines(file)'
    Tests = 'MigrationTests.GeneratedFileNote_LeavesOutDesignersAndToolOutput_NoBuildWrites'
}
