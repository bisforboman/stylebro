# An up-to-date skill is not written again.
@{
    File = 'src/StyleBro.Migrate/SkillFile.cs'
    Find = 'var changed = ours && existing != Text;'
    Replace = 'var changed = ours;'
    Tests = 'MigrationTests.Skill_DryRunNamesIt'
}
