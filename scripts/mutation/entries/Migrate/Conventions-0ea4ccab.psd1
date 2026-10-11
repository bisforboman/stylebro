# Trial of 0.4.0-alpha.1 (2026-10-10): rules off without a majority, few places, nested Directory.Build.props, vendored code
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (written.Count + matched + lists.TurnedOff.Count + lists.Unfollowed.Count + lists.BothAllowed.Count + lists.TooFew.Count == 0)'
    Replace = 'if (true)'
    Tests = 'MigrationTests'
}
