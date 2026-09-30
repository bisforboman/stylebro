# Repos included in docs/stylecop-mapping.md. Each needs data/<Name>-configs.csv and data/<Name>-counts.csv
# (from Invoke-RepoSurvey.ps1). ProdConfigs lists the config files that apply to production code, lowest
# precedence first (.editorconfig beats rulesets and global configs, so it goes last).
@{
    Repos = @(
        @{
            Name        = 'otel'
            Label       = 'OTel'
            Title       = '[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet) (`ea1e3f3`, StyleCop 1.2.0-beta.556)'
            ProdConfigs = @('build\OpenTelemetry.prod.ruleset', '.editorconfig')
            Note        = '10 of OpenTelemetry''s 81 projects (examples and some tests) did not compile in the survey (CS1705 in a shallow clone) and are missing from its counts.'
        }
        @{
            Name        = 'polly'
            Label       = 'Polly'
            Title       = '[Polly](https://github.com/App-vNext/Polly) (`0275bc2`, StyleCop 1.2.0-beta.556)'
            ProdConfigs = @('eng\analyzers\Stylecop.globalconfig', '.editorconfig')
        }
        @{
            Name        = 'app'
            Label       = 'App'
            Title       = 'a private 30-project app (StyleCop 1.1.118)'
            ProdConfigs = @('.editorconfig')
        }
    )
}
