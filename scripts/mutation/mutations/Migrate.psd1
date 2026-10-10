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
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = ': count[1] >= Share * total ? 1 : count[0] >= Share * total ? 0 : -1;'; Replace = ': count[1] > count[0] ? 1 : 0;'; Tests = 'MigrationTests' }
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

        # stylebro-migrate format: kept findings don't fail it, files left to change do (2026-10-10)
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'if (copyCode != 0 || changed > 0)'; Replace = 'if (copyCode != 0)'; Tests = 'KeptFindingsTests' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'return NotCleanExitCode;'; Replace = 'return 0;'; Tests = 'KeptFindingsTests' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'lines.Where(l => !IsKeptLine(l, keys))'; Replace = 'lines'; Tests = 'KeptFindingsTests' }

        # Trial of 0.4.0-alpha.1 (2026-10-10): rules off without a majority, few places, nested Directory.Build.props, vendored code
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'var winner = total < MinimumSample ? (count[0] == 0 ? 1 : count[1] == 0 ? 0 : -1)'; Replace = 'var winner = total < MinimumSample ? -1'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = '!(isSet?.Invoke(off.Substring(0, off.IndexOf(''='')).Trim()) ?? false)'; Replace = 'true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'else if (total < MinimumAgreeing)'; Replace = 'else if (false)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (written.Count + matched + turnedOff.Count + tooFew.Count == 0)'; Replace = 'if (true)'; Tests = 'MigrationTests' }
        # init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'found = winner < 0 ? Plural(total, "place") : found;'; Replace = 'found = found;'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'yield return matched == 0 ?'; Replace = 'yield return true ?'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = ': written.Count == 0 ? $"{kept}, all already'; Replace = ': false ? $"{kept}, all already'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'while (line.Length > Width)'; Replace = 'while (false)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = '(count == 1 ? string.Empty : "s")'; Replace = '"s"'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = '(!parenthesized || level > outer || (level == outer && operand == binary.Left))'; Replace = 'true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'Regex.Replace(File.ReadAllText(candidate), "<!--.*?-->", string.Empty, RegexOptions.Singleline)'; Replace = 'File.ReadAllText(candidate)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = '? Nearest(Path.GetDirectoryName(directory)) : candidate;'; Replace = '? candidate : candidate;'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = '|| all.All(s.Value.Contains)'; Replace = '|| true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/InitCommand.cs'; Find = 'if (!EnforcesCodeStyleInBuild(root))'; Replace = 'if (true)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/PreviewCommand.cs'; Find = 'if (NoNewlineAfter(''-'') != NoNewlineAfter(''+''))'; Replace = 'if (false)'; Tests = 'MigrationTests' }

        # For agents (2026-10-10): --json, format --files, the AGENTS.md section
        @{ File = 'src/StyleBro.Migrate/Program.cs'; Find = 'Console.SetOut(Console.Error);'; Replace = ''; Tests = 'MigrationTests.Json_Main' }
        @{ File = 'src/StyleBro.Migrate/JsonReport.cs'; Find = 'if (afterSetting)'; Replace = 'if (false)'; Tests = 'MigrationTests.Json_Init' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'var runArgs = report is null ? args : args.Concat(new[] { "--report", report }).ToArray();'; Replace = 'var runArgs = args;'; Tests = 'MigrationTests.Json_Format' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)'; Replace = 'false'; Tests = 'MigrationTests.Files_' }
        @{ File = 'src/StyleBro.Migrate/AgentsFile.cs'; Find = 'if (!requested)'; Replace = 'if (false)'; Tests = 'MigrationTests.AgentsMd_' }
        @{ File = 'src/StyleBro.Migrate/AgentsFile.cs'; Find = 'if (changed)'; Replace = 'if (true)'; Tests = 'MigrationTests.AgentsMd_' }
    )
}
