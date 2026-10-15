# Trial 22: the same parts said once
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var described = partitions.Select(parts => string.Join(" | ", parts.Select(Names))).Distinct(StringComparer.Ordinal).ToList();'
    Replace = 'var described = partitions.Select(parts => string.Join(" | ", parts.Select(Names))).ToList();'
    Tests = 'MigrationTests.Format_NamesEachGroupOfProjectsThatRunApart'
}
