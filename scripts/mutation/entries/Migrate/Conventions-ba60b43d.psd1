# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& !"aeiou".Contains(noun[^2])'
    Replace = ''
    Tests = 'MigrationTests.Plural_FollowsEnglish'
}
