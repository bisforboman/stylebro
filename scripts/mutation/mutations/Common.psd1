# Mutations for Common (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # Files from NuGet packages (PackageFileSuppressor)
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = '? roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))'; Replace = '? true'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = 'file.IndexOf("/contentFiles/", StringComparison.OrdinalIgnoreCase) >= 0'; Replace = 'false'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = 'return roots.Count > 0'; Replace = 'return false'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = 'if (isPackageFile)'; Replace = 'if (true)'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = ".Replace('\\', '/').TrimEnd('/'))"; Replace = ".Replace('\\', '/'))"; Tests = 'PackageFileSuppressorTests' }

        # Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
        @{ File = 'src/StyleBro.CodeFixes/NodeCodeFixProvider.cs'; Find = '.ConfigureAwait(false)).Count == 0)'; Replace = '.ConfigureAwait(false)).Count < 0)'; Tests = 'SonarIdsTests.S1066_NoActionWhereBro1149Skips' }

        # Generated code and C# versions (real-world: eShopOnWeb's Razor pages, Mapperly's generated accessors, LibGit2Sharp's net472)
        @{ File = 'src/StyleBro.CodeFixes/LinkedFileFixAllProvider.cs'; Find = '.Min();'; Replace = '.Max();'; Tests = 'NullCheckTests' }

        # Sonar rule defaults by package version (BRO1150)
        @{ File = 'src/StyleBro.Analyzers/SonarRules.cs'; Find = '&& GetMajorVersion(paths) is < 10;'; Replace = ';'; Tests = 'WhereBeforeTerminalTests.SonarCollectionRules' }

        # the names the renamers check (RepositoryNames, shared with format's per-framework runs)
        @{ File = 'src/StyleBro.Analyzers/RepositoryNames.cs'; Find = 'else if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "nameof"'; Replace = 'else if (false'; Tests = 'FieldNamingTests.NameofInAnotherType_KeepsTheName' }
    )
}
