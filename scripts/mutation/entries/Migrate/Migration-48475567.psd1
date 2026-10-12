# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'code.All(f => others.Any(o => IsUnder(f, o)))'
    Replace = 'code.Any(f => others.Any(o => IsUnder(f, o)))'
    Tests = 'MigrationTests.TheFieldStyle_FollowsSA1309WhereTheCodeIs'
}
