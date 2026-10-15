# Trial 22: an item naming a file isn't a build step writing it (kafkaflow)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& !Regex.IsMatch(l, @"<(Compile|None|Content|EmbeddedResource|DependentUpon)\b", RegexOptions.IgnoreCase));'
    Replace = ');'
    Tests = 'MigrationTests.GeneratedFileNote_LeavesOutVisualStudioToolOutput'
}
