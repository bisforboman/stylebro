# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (project is not null && Regex.Match(project, @"<Project\b[^>]*?/>") is { Success: true } closed)'
    Replace = 'if (project is not null && Regex.Match(project, @"^$") is { Success: false } closed)'
    Tests = 'MigrationTests.AddItem_PutsTheGroupOnLinesOfItsOwn'
}
