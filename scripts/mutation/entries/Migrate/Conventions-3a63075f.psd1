# Trial 18: always with interface members the code writes without modifiers (BaGetter)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '|| counts[InterfaceAccessKey] is not [var with, var without] || with + without < MinimumAgreeing || Winner(with, without) == 0)'
    Replace = '|| counts[InterfaceAccessKey] is not [var with, var without])'
    Tests = 'MigrationTests.Init_FollowsInterfaceMembersWithoutModifiers_WhenNothingEnforcesAlways'
}
