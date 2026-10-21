# Trial 39: the key stays true where the code separates root namespaces.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = ' || SeparatesUsingGroups(c[0], c[1]))'
    Replace = ')'
    Tests = 'MigrationTests.Migration_UsingGroups_FollowTheCode'
}
