# 'always' alone isn't enforced in the build: IDE0040 must be a warning or an error (trial 20)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'EnforcesCodeStyleInBuild(root) && IsEnforced(settings, "IDE0040")'
    Replace = 'EnforcesCodeStyleInBuild(root)'
    Tests = 'MigrationTests.Init_FollowsInterfaceMembersWithoutModifiers'
}
