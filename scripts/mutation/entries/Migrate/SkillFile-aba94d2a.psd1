# The skill is opt-in (--skill).
@{
    File = 'src/StyleBro.Migrate/SkillFile.cs'
    Find = '        if (!requested)
        {
            return lines;
        }'
    Replace = '        if (requested && !requested)
        {
            return lines;
        }'
    Tests = 'MigrationTests.Skill_IsOptIn'
}
