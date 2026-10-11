# trial 6 (2026-10-11): framework spellings, kept reasons, the package under central package management, the preset opt-out, init's next step
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.SelectMany(p => p.Value.Select(f => (Framework: NormalizeFramework(f), Project: p.Key)))'
    Replace = '.SelectMany(p => p.Value.Select(f => (Framework: f, Project: p.Key)))'
    Tests = 'MigrationTests.Format_SpellingsOfOneFramework'
}
