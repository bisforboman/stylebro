# a failed design-time build is a load failure
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return FirstBuildError(e.Message) ?? "its design-time build failed";'
    Replace = 'return null;'
    Tests = 'FormatHostTests.AProjectWhoseDesignTimeBuildFails_IsNamed_AndTheCheckIsNotVerified'
}
