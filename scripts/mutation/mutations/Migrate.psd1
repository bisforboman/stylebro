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
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = ': second >= Share * total ? 1 : first >= Share * total ? 0 : -1;'; Replace = ': second > first ? 1 : 0;'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (Migration.IsGeneratedOrVendored(Path.GetRelativePath(root, file), text))'; Replace = 'if (false)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (IsGeneratedOrVendored(relative, text))'; Replace = 'if (false)'; Tests = 'MigrationTests' }

        # a hand-written Migrations folder counts; only EF Core's files are skipped, like the generated_code sections (2026-10-10)
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = '            || IsEfMigration(text)'; Replace = ''; Tests = 'MigrationTests' }

        # stylebro-migrate follows a SonarQube setup (2026-10-09)
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '.Where(f => !Path.GetFileName(f).Contains("none", StringComparison.OrdinalIgnoreCase))'; Replace = ''; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = 'severities[id] = kind.Max('; Replace = 'severities[id] = kind.Min('; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '(string?)r.Attribute("AnalyzerId") == "SonarAnalyzer.CSharp"'; Replace = 'true'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '(string?)r.Element("repositoryKey") == "csharpsquid"'; Replace = 'true'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = 'optionOnly && !ruleOn(rule)'; Replace = 'false'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = '(StyleCopSetup.ParseSeverity(ValueOf(lines[existing])) ?? Severity.None) < severity'; Replace = 'false'; Tests = 'MigrationTests.Sonar_' }

        # SX1309S counts as covered only with '_camelCase' (2026-10-09)
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (result.FieldStyle != "_camelCase")'; Replace = 'if (false)'; Tests = 'MigrationTests.StaticUnderscoreAlone' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'lines.Add($"{StyleBro.Analyzers.Naming.FieldNames.MutableStaticStyleKey} = {result.FieldStyle}");'; Replace = ''; Tests = 'MigrationTests.MutableStaticStyle_IsPinned' }

        # stylebro-migrate format: kept findings don't fail it, files left to change do (2026-10-10)
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'if (copyCode != 0 || changed > 0)'; Replace = 'if (copyCode != 0)'; Tests = 'KeptFindingsTests' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'return NotCleanExitCode;'; Replace = 'return 0;'; Tests = 'KeptFindingsTests' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'lines.Where(l => !IsKeptLine(l, keys))'; Replace = 'lines'; Tests = 'KeptFindingsTests' }

        # Trial of 0.4.0-alpha.1 (2026-10-10): rules off without a majority, few places, nested Directory.Build.props, vendored code
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = ': total < MinimumSample ? (count[0] == 0 ? 1 : count[1] == 0 ? 0 : -1)'; Replace = ': total < MinimumSample ? -1'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'convention.Off is { } off && !IsSet(off)'; Replace = 'convention.Off is { } off'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'else if (total < MinimumAgreeing)'; Replace = 'else if (false)'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (written.Count + matched + lists.TurnedOff.Count + lists.Unfollowed.Count + lists.BothAllowed.Count + lists.TooFew.Count == 0)'; Replace = 'if (true)'; Tests = 'MigrationTests' }
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

        # Trial of 0.5.0-alpha.1 (2026-10-10): keys the code contradicts, one-line statements, BRO1601, '#if' code, Sonar NoWarn
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'Contradicted(convention, convention.Values[winner].Value, repository) is { } value && !IsSet(Off(convention.Enforcer!))'; Replace = 'Contradicted(convention, convention.Values[winner].Value, repository) is { } value'; Tests = 'MigrationTests.Conventions_AKeyTheCodeContradicts' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (total >= MinimumAgreeing && winner >= 0 && Contradicted('; Replace = 'if (winner >= 0 && Contradicted('; Tests = 'MigrationTests.Conventions_AKeyTheCodeContradicts' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'return value != code && convention.Values.Any(v => v.Value == value) && (convention.EnforcedValues?.Contains(value) ?? true) ? value : null;'; Replace = 'return value != code ? value : null;'; Tests = 'MigrationTests.Conventions_AKeyTheCodeContradicts' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'var winner = convention.KeepAny && count[1] >= MinimumAgreeing ? 1'; Replace = 'var winner = false ? 1'; Tests = 'MigrationTests.Conventions_KeepStatements' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (body is not (BlockSyntax or IfStatementSyntax or UsingStatementSyntax))'; Replace = 'if (true)'; Tests = 'MigrationTests.Conventions_CountStatements' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'trees.Where(t => only?.Contains(t) ?? true)'; Replace = 'trees'; Tests = 'MigrationTests.Conventions_CountTheMembersBro1601Checks' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (!projectFound && Directory.GetFiles(current, "*.csproj")'; Replace = 'if (Directory.GetFiles(current, "*.csproj")'; Tests = 'MigrationTests.Init_CountsBro1601OnlyWhereDocumentationIsGenerated' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'offLine == $"{convention.Key} = {convention.Values[winner].Value}")'; Replace = 'false)'; Tests = 'MigrationTests.Conventions_TurnBro1601Off' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'bool Skip(int position) => only is not null && !only.Any(s => s.Contains(position));'; Replace = 'bool Skip(int position) => false;'; Tests = 'MigrationTests.Conventions_CountCodeInactive' }
        @{ File = 'src/StyleBro.Migrate/Suppressions.cs'; Find = 'SonarSetup.Mapping.Where(m => sonar?.IsOn(m.Sonar) ?? true)'; Replace = 'SonarSetup.Mapping'; Tests = 'MigrationTests.Init_AddsNoSuppressionsForASonarRuleThatIsOff' }

        # Trial of 0.5.0-alpha.1 (2026-10-10): settings only where StyleCop runs, sort keys, the package next to StyleCop's,
        # no whitespace pass with IDE0055 off, old Mac line endings, verify's own errors, kept findings marked as kept
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'uses[project] = references.Count > 0 && removes.Count == 0;'; Replace = 'uses[project] = references.Count > 0;'; Tests = 'MigrationTests.Settings_' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'parent.Length > rootFolder.Length && !HasOther(parent)'; Replace = 'parent.Length > rootFolder.Length'; Tests = 'MigrationTests.Settings_' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'else if (covered.Count == folders.Count)'; Replace = 'else if (false)'; Tests = 'MigrationTests.Settings_' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'else if (covered.Count > 0)'; Replace = 'else if (false)'; Tests = 'MigrationTests.AScopeAbove' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '.FirstOrDefault(start => !siblings.Any(s => s.StartsWith(start, StringComparison.OrdinalIgnoreCase)));'; Replace = '.FirstOrDefault();'; Tests = 'MigrationTests.TheSection_' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'group.Key.Start is { } start && group.Count() > 1'; Replace = 'group.Key.Start is { } start'; Tests = 'MigrationTests.TheSection_' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (setup.Scopes.Any(scope => setup.For(scope) is var inScope'; Replace = 'if (false && setup.Scopes.Any(scope => setup.For(scope) is var inScope'; Tests = 'MigrationTests.ScopesThatDontSortUsings' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (folders is not null && !folders.Any('; Replace = 'if (false && !folders!.Any('; Tests = 'MigrationTests.Settings_' }
        @{ File = 'src/StyleBro.Migrate/PreviewCommand.cs'; Find = 'var targets = styleCop.Count > 0 ? styleCop : Migration.PropsFiles(root);'; Replace = 'var targets = Migration.PropsFiles(root);'; Tests = 'MigrationTests.ThePackage_' }
        @{ File = 'src/StyleBro.Migrate/PreviewCommand.cs'; Find = 'if (Sets(file, "false"))'; Replace = 'if (false)'; Tests = 'MigrationTests.ThePackage_' }
        @{ File = 'src/StyleBro.Migrate/PreviewCommand.cs'; Find = 'foreach (var file in removes)'; Replace = 'foreach (var file in Array.Empty<string>())'; Tests = 'MigrationTests.ThePackage_' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'return values.Count > 0 && values.All(v => v == "none");'; Replace = 'return values.All(v => v == "none");'; Tests = 'MigrationTests.Format_SkipsTheWhitespacePass' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'if (at + 1 == bytes.Length || bytes[at + 1] != ''\n'')'; Replace = 'if (at + 1 == bytes.Length)'; Tests = 'MigrationTests.Format_NamesFilesWithOldMacLineEndings' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'if (code != NotCleanExitCode)'; Replace = 'if (false)'; Tests = 'MigrationTests.Verify_AnUnknownOption' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'lines.Select(l => MarkKept(l, keys)).ToList().ForEach(log);'; Replace = 'lines.ForEach(log);'; Tests = 'KeptFindingsTests' }

        # a faster first run: only the first run restores, later runs check all frameworks at once, the preview's first run reports (2026-10-10)
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'r.Code == 0 && '; Replace = ''; Tests = 'MigrationTests.Format_CleanCheck' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'SkippedProject(l) is not null || '; Replace = ''; Tests = 'MigrationTests.Format_CleanCheck' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'next.Contains(CheckFirstOption) ? next : '; Replace = ''; Tests = 'KeptFindingsTests.Format_StillChangingAfterTheLastRun_IsNotClean' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = 'args.Contains("--no-restore") ? args : '; Replace = ''; Tests = 'KeptFindingsTests.Format_StillChangingAfterTheLastRun_IsNotClean' }
        @{ File = 'src/StyleBro.Migrate/FormatCommand.cs'; Find = '            args = NextRun(args);'; Replace = ''; Tests = 'KeptFindingsTests.Format_StillChangingAfterTheLastRun_IsNotClean' }
        @{ File = 'src/StyleBro.Migrate/PreviewCommand.cs'; Find = ': FormatCommand.NextRun(formatArgs.ToArray());'; Replace = ': formatArgs;'; Tests = 'MigrationTests.Preview_AppliesInitAndFormatToACopy' }

        # init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = '&& !info.Contains("eol=", StringComparison.Ordinal)'; Replace = ''; Tests = 'MigrationTests.Conventions_Converted_' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = '_ = converted.Contains(Path.GetFullPath(file)) ?'; Replace = '_ = false ?'; Tests = 'MigrationTests.Init_LeavesTheLineEndingUnset_WhenGitConvertsIt' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (isSet?.Invoke(SortUsingsKey) == true || isSet?.Invoke("dotnet_separate_import_directive_groups") == true)'; Replace = 'if (isSet?.Invoke(SortUsingsKey) == true)'; Tests = 'MigrationTests.Conventions_DecideTheUsingOrder' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'var system = sort != 0 || apart < MinimumAgreeing ? 0 : Winner(c[0], c[1]);'; Replace = 'var system = 0;'; Tests = 'MigrationTests.Conventions_DecideTheUsingOrder' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'if (systemFirst && group < 2 &&'; Replace = 'if (false &&'; Tests = 'MigrationTests.Conventions_CountTheUsingOrder' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = '.Select(l => l.Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None)).ToList())'; Replace = '.Select(l => l.ToList())'; Tests = 'MigrationTests.Conventions_CountTheUsingOrder' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'keys.All(k => k is not null) && keys.Any(k => k!.Value.CompareTo(keys[0]!.Value) != 0)'; Replace = 'keys.All(k => k is not null)'; Tests = 'MigrationTests.Conventions_CountOverloadsBro1001CouldSplit' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'group[^1].Index - group[0].Index == group.Count - 1'; Replace = 'true'; Tests = 'MigrationTests.Conventions_CountOverloadsBro1001CouldSplit' }
        @{ File = 'src/StyleBro.Migrate/Conventions.cs'; Find = 'convention.Off is { } offLine && RuleOf(offLine) is not null &&'; Replace = 'convention.Off is { } offLine &&'; Tests = 'MigrationTests.Conventions_BracesWithoutAMajority' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = 'LoadMapping().Where(m => m.Rule != "-").ToList();'; Replace = 'LoadMapping();'; Tests = 'MigrationTests.Sonar_' }
        @{ File = 'src/StyleBro.Migrate/SonarSetup.cs'; Find = 'applied.NotCovered.AddRange(NotCovered.Where(m => IsOn(m.Sonar)).Select(m => (m.Sonar, m.Note!)));'; Replace = ''; Tests = 'MigrationTests.Sonar_S3260' }
    )
}
