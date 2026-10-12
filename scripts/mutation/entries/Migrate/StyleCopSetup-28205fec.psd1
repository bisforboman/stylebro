# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '            if (line.StartsWith(''[''))'
    Replace = '            if (false)'
    Tests = 'MigrationTests.EditorConfigRoots_'
}
