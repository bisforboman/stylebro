# A multi-targeted project's platform frameworks are evaluated for missing workloads (trial 35, sqlite-net).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': PlatformFrameworks(frameworks).SelectMany(f => FrameworkWorkloads(fullPath, f))'
    Replace = ': packs.Where(_ => true)'
    Tests = 'MissingWorkloadPacksOf'
}
