# A SKILL.md bro didn't write stays as it is.
@{
    File = 'src/StyleBro.Migrate/SkillFile.cs'
    Find = 'var ours = existing is null || existing.Contains(Marker, StringComparison.Ordinal);'
    Replace = 'var ours = true;'
    Tests = 'MigrationTests.Skill_SomeoneElsesSkillMd'
}
