# Trial 18: a key the build enforces stays the team's
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (enforcedInBuild || !repository.TryGetValue(AccessModifiersKey, out var setting)'
    Replace = 'if (!repository.TryGetValue(AccessModifiersKey, out var setting)'
    Tests = 'MigrationTests.Init_FollowsInterfaceMembersWithoutModifiers_WhenNothingEnforcesAlways'
}
