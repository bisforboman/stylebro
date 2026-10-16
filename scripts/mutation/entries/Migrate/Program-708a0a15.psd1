# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (command is null && options.Any(a => a.Split(''='')[0] == InitCommand.StyleOption))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.TheMigration_RejectsAStyle_AndRemovesOne'
}
