# Only frameworks naming a platform can need a workload (trial 35).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'frameworks.Where(f => f.Contains(''-'', StringComparison.Ordinal))'
    Replace = 'frameworks.Where(f => true)'
    Tests = 'PlatformFrameworks_AreTheOnesNamingAPlatform'
}
