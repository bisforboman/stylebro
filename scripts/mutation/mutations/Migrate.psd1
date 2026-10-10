# Mutations for Migrate (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = '.Where(d => !d.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))'; Replace = ''; Tests = 'MigrationTests.EveryStyleBroRule_NamesTheStyleCopRulesItReplaces' }

        # stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '&& !IsNestedRepository(child)'; Replace = ''; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '(defaultSeverity > Severity.None || severity == Severity.None)'; Replace = 'true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (setup.IsOn("SA1633") && textOff.Count == 0)'; Replace = 'if (setup.IsOn("SA1633"))'; Tests = 'MigrationTests' }

        # stylebro-migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
        # folder rulesets, init's field style
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = ' || CompanyName(setup) is null))'; Replace = '))'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '!config.Bulk.Contains(p.Key) || !specific.Contains(p.Key)'; Replace = 'true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = ' || !order.Contains("accessibility")'; Replace = ''; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'rulesets.Add([]);'; Replace = ''; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'var winner = count[1] >= Share * total ? 1 : count[0] >= Share * total ? 0 : -1;'; Replace = 'var winner = count[1] > count[0] ? 1 : 0;'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (!Migration.IsGeneratedOrVendored(Path.GetRelativePath(root, file), text))'; Replace = 'if (true)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (IsGeneratedOrVendored(Path.GetRelativePath(root, file), text))'; Replace = 'if (false)'; Tests = 'MigrationTests' }

        # stylebro-migrate follows a SonarQube setup (2026-10-09)
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '.Where(f => !Path.GetFileName(f).Contains("none", StringComparison.OrdinalIgnoreCase))'; Replace = ''; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = 'severities[id] = kind.Max('; Replace = 'severities[id] = kind.Min('; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '(string?)r.Attribute("AnalyzerId") == "SonarAnalyzer.CSharp"'; Replace = 'true'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '(string?)r.Element("repositoryKey") == "csharpsquid"'; Replace = 'true'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = 'optionOnly && !ruleOn(rule)'; Replace = 'false'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '(StyleCopSetup.ParseSeverity(ValueOf(lines[existing])) ?? Severity.None) < severity'; Replace = 'false'; Tests = 'MigrationTests.Sonar_' }

        # SX1309S counts as covered only with '_camelCase' (2026-10-09)
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (result.FieldStyle != "_camelCase")'; Replace = 'if (false)'; Tests = 'MigrationTests.StaticUnderscoreAlone' }
    )
}
