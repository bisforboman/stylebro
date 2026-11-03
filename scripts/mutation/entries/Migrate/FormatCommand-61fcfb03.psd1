# A run without a framework still probes a project with one entry in TargetFrameworks for it (trial 66, moq4).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '? (own.Count > 0 ? own[0] : string.Empty)'
    Replace = '? (own.Count > 1 ? own[0] : string.Empty)'
    Tests = 'LoadProbeFramework'
}
