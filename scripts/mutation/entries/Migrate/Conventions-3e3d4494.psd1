# Trial 10 (2026-10-12): root = true folders, end_of_line the files contradict, migrate braces, s_ prefixes and IDE0049
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = ' || files == value'
    Replace = ''
    Tests = 'MigrationTests.ContradictedEnding_'
}
