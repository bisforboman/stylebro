# Mutations for Naming (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # BRO1309: snake_case names; a property kept by a string keeps its type's other properties
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'if (reason is null && symbol is IPropertySymbol'; Replace = 'if (false && symbol is IPropertySymbol'; Tests = 'PascalCaseNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '.Any(p => PascalCaseNamingAnalyzer.GetNewName(p.Name) is not null && FindInCode(p, names).Reason is not null)'; Replace = '.Any(p => FindInCode(p, names).Reason is not null)'; Tests = 'PascalCaseNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = "symbol.Name.TrimStart('_').IndexOf('_') < 0 ? GetNewName(symbol.Name) : null;"; Replace = 'GetNewName(symbol.Name);'; Tests = 'PascalCaseNamingTests' }

        # BRO1303/BRO1306/BRO1307 (fields)
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'var newName = isProtected && field.IsReadOnly ?'; Replace = 'var newName = false ?'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '&& field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;'; Replace = '&& field.DeclaredAccessibility != Accessibility.Private;'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'GetNamingRules(options, m => m.Length == 0)'; Replace = 'GetNamingRules(options, m => true)'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'; Find = '&& !(rule == FieldRule.Prefix && FieldNames.IsPrefixRequiredByNamingRule(field.Name.Substring(0, 2), options))'; Replace = ''; Tests = 'FieldNamingTests' }

        # BRO1302: constructor parameters that name a member
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '            || NamesAMember(parameter)'; Replace = ''; Tests = 'CamelCaseNamingTests' }

        # BRO1311 (tuple element casing)
        @{ File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'; Find = '|| TupleElementNames.InheritsNames(element, context.SemanticModel, context.CancellationToken)'; Replace = ''; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = ': await AddsErrorsAsync(solution, renamed, cancellationToken).ConfigureAwait(false) ? (null, KeptReason.AddsErrors)'; Replace = ': false ? (null, KeptReason.AddsErrors)'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'; Find = '&& HasTupleNames(b.OriginalDefinition)'; Replace = ''; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = 'if (InferredNameStart(name) is { } start)'; Replace = 'if (InferredNameStart(name) is { } start && false)'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = 'if (target is not null && target.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))'; Replace = 'if (true)'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = '&& element.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))'; Replace = ')'; Tests = 'TupleElementNamingTests' }

        # BRO1310 (Hungarian prefixes)
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '|| allowed.Contains(match.Groups["prefix"].Value)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '&& !char.IsUpper(name[0])'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '&& !HungarianNames.IsInNativeMethods(context.Node)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = ', name => HungarianNames.GetVariableName(name, hungarian))'; Replace = ')'; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'declaration.ContainsDirectives ? TreeWalk.Trivia('; Replace = 'false ? TreeWalk.Trivia('; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'facts.Texts.Add(list[j].ToString());'; Replace = ''; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '|| HungarianNames.IsInNativeMethods(field)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'if (GetNewName(f, key.Item1, hungarian) is { } name)'; Replace = 'if (GetNewName(f, key.Item1) is { } name)'; Tests = 'HungarianNamingTests' }

        # BRO1306 option: camelCase private constants / static readonly fields when a naming rule asks for it
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'field.DeclaredAccessibility != Accessibility.Private || !IsSourceField(field)'; Replace = '!IsSourceField(field)'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '|| !modifiersMatch(modifiers))'; Replace = ')'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '.ThenByDescending(r => r.Modifiers)'; Replace = ''; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '.OrderBy(r => r.Priority)
            .ThenByDescending'; Replace = '.OrderBy(r => 0)
            .ThenByDescending'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '            _ => null,
        };
    }'; Replace = '            _ => FieldStyle.CamelCase,
        };
    }'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'm => m.Contains("const") && m.All(x => x is "const" or "static")'; Replace = 'm => m.Length > 0'; Tests = 'FieldNamingTests' }

        # BRO1313 (parameter names like the base)
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '|| node.Identifier.ValueText.Trim(''_'').Length == 0'; Replace = ''; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '|| member is IMethodSymbol { PartialDefinitionPart: not null } or IMethodSymbol { PartialImplementationPart: not null }
            || (!member.IsOverride'; Replace = '|| (!member.IsOverride'; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = 'if (result is not null && result != name)'; Replace = 'if (false)'; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '|| !CamelCaseNames.IsUsableName(newName)'; Replace = ''; Tests = 'ParameterMatchesBaseTests' }

        # Names a rename never produces: 'field' (C# 14 keyword in accessors), 'value' inside a property/indexer/event
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = ' && name != "field";'; Replace = ';'; Tests = 'CamelCaseNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = ' && name != "field";'; Replace = ';'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = ' && name != "field";'; Replace = ';'; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = ' && name != "field";'; Replace = ';'; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = 'if (newName == "value" && scope is BasePropertyDeclarationSyntax)'; Replace = 'if (false)'; Tests = 'CamelCaseNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '|| !CamelCaseNames.CanRename(node, oldName, newName, getNewName)'; Replace = ''; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '|| CamelCaseNames.IsNameObservable(node.Parent!.Parent!, oldName, context.SemanticModel, context.CancellationToken))'; Replace = ')'; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = '&& a.ConstructorArguments[0].Value as string == target.Name'; Replace = '&& false'; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '|| (keepObservableNames && current is IParameterSymbol && await IsObservableAsync(current).ConfigureAwait(false))'; Replace = ''; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = 'if (depth < 8 && baseParameter'; Replace = 'if (false && baseParameter'; Tests = 'ParameterMatchesBaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '&& CamelCaseNames.CanRename(declaration, name, renamed, getNewName) ? renamed'; Replace = '? renamed'; Tests = 'ParameterMatchesBaseTests' }

        # BRO1314 (Async suffix): the analyzer's skips, then the rename's
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '&& method.Name != "Main"'; Replace = ''; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '&& method.Name.IndexOf("Async", System.StringComparison.Ordinal) < 0'; Replace = ''; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = 'return method.MethodKind == MethodKind.Ordinary'; Replace = 'return true'; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = 'or "ExcludeFromCodeCoverageAttribute" or "PureAttribute")'; Replace = 'or "ExcludeFromCodeCoverageAttribute" or "PureAttribute" or "FactAttribute")'; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '&& !IsEventHandler(method)'; Replace = ''; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = 'if (current.Name is "Controller" or "ControllerBase" or "Hub"'; Replace = 'if (false'; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '|| current.Name.EndsWith("Controller", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '|| current.GetAttributes().Any(a => a.AttributeClass?.Name == "ApiControllerAttribute")'; Replace = ''; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '? type.Arity <= 1 && ns.ToDisplayString() == "System.Threading.Tasks"'; Replace = '? true'; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = 'newName = (casingOn ? newName ?? symbol.Name : symbol.Name) + "Async";'; Replace = 'newName = (newName ?? symbol.Name) + "Async";'; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'if (symbols.Any(s => CamelCaseNamingAnalyzer.GetBaseMembers(s).Any(b => !renamed.Contains(b.OriginalDefinition.ToDisplayString()))))'; Replace = 'if (false)'; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'return (itemChanges, kept, null);'; Replace = 'continue;'; Tests = 'LinkedFileFixAllTests' }

        # BRO1312 (namespace names): the analyzer's skips, then the rename's
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| allowed.Contains(oldName)'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = 'if (parent.GetMembers(newName).Any()'; Replace = 'if (false'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| !NamespaceNames.IsOnlyFrom(ns, a => SymbolEqualityComparer.Default.Equals(a, context.Compilation.Assembly))'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| IsInRootNamespace(fullName, rootNamespace)'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| declared.Locations.Any(l => l.SourceTree is { } tree && NamespaceNames.IsGenerated(tree)))'; Replace = ')'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'is { } text && fullNameInText.Match(text.ToString()) is { Success: true } match)'; Replace = 'is { } text && fullNameInText.Match(text.ToString()) is { Success: false } match)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if (NamespaceNames.Find(global, oldFullName) is { } ns && !NamespaceNames.IsOnlyFrom(ns, a => assemblies.Contains(a.Name)))'; Replace = 'if (false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if ((parentName.Length == 0 ? global : NamespaceNames.Find(global, parentName))?.GetMembers(newPart).Any() == true)'; Replace = 'if (false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = '&& fullNameInText.IsMatch(t.ValueText))'; Replace = '&& false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if (root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && partInText.IsMatch(t.ToString())))'; Replace = 'if (false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if (document is null || generated)'; Replace = 'if (document is null)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if (!generated && root'; Replace = 'if (root'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if (IsLookedUp(name) && !model.LookupSymbols(name.SpanStart, name: newPart).IsEmpty)'; Replace = 'if (false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'else if (text == newPart && IsLookedUp(name) && SeesMembersOf(model, name, parentName)'; Replace = 'else if (false'; Tests = 'NamespaceNamingTests' }

        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '&& !HungarianNames.IsExternParameter(context.Node)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '(modifiers.Any(SyntaxKind.ExternKeyword)'; Replace = '(false'; Tests = 'HungarianNamingTests' }

        # BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards

        # Generated code and C# versions (real-world: eShopOnWeb's Razor pages, Mapperly's generated accessors, LibGit2Sharp's net472)
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = ' || NamespaceNames.HasGeneratedPart(type))'; Replace = ')'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '|| ((symbol as INamedTypeSymbol ?? symbol.ContainingType) is { } owner && NamespaceNames.HasGeneratedPart(owner))'; Replace = ''; Tests = 'PascalCaseNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'; Find = '&& (symbol.DeclaringType is not { } owner || !NamespaceNames.HasGeneratedPart(owner))'; Replace = ''; Tests = 'PrefixNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'if (document is null or SourceGeneratedDocument'; Replace = 'if (document is null'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '|| NamespaceNames.IsGenerated(tree))'; Replace = '|| false)'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'documents.AddRange(await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false));'; Replace = ''; Tests = 'FieldNamingTests' }

        # stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'if (options.TryGetValue(StaticStyleKey, out var value))'; Replace = 'if (false && options.TryGetValue(StaticStyleKey, out var value))'; Tests = 'FieldNamingTests' }

        # Names nameof(...) produces keep them (Ocelot)
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = '&& !names.Nameof.Contains(oldName)'; Replace = ''; Tests = 'CamelCaseNamingTests.NamesInNameof_KeepTheirName' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = 'access.Name == name ? (ExpressionSyntax)access : name'; Replace = 'name'; Tests = 'FieldNamingTests.NamesInNameof_KeepTheirName' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '|| CamelCaseNames.IsNameofName(token)'; Replace = ''; Tests = 'FieldNamingTests.NamesInNameof_KeepTheirName' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'else if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "nameof"'; Replace = 'else if (false'; Tests = 'FieldNamingTests.NameofInAnotherType_KeepsTheName' }

        # Public API is renamed only with stylebro_rename_public_api = true
        @{ File = 'src/StyleBro.Analyzers/Naming/PublicApi.cs'; Find = 'value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)'; Replace = 'true'; Tests = 'FieldNamingTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PublicApi.cs'; Find = '|| ns.GetNamespaceMembers().Any(IsVisible)'; Replace = ''; Tests = 'NamespaceNamingTests.NamespacesOfPublicTypes_AreLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PublicApi.cs'; Find = 'Accessibility.Public or Accessibility.Protected or'; Replace = 'Accessibility.Public or'; Tests = 'CamelCaseNamingTests.PublicApiParameters_AreLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'; Find = '&& PublicApi.CanRename(field, options)'; Replace = ''; Tests = 'FieldNamingTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '|| !PublicApi.CanRename(symbol, '; Replace = '|| false && !PublicApi.CanRename(symbol, '; Tests = 'PascalCaseNamingTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'; Find = '&& PrefixNames.GetInterfaceName(symbol.Name) is { } newName
            && PublicApi.CanRename(symbol, context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree))'; Replace = '&& PrefixNames.GetInterfaceName(symbol.Name) is { } newName
            && true'; Tests = 'PrefixNamingTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'; Find = '&& !InheritsName(symbol)
            && PublicApi.CanRename(symbol, context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree))'; Replace = '&& !InheritsName(symbol)
            && true'; Tests = 'PrefixNamingTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| !PublicApi.CanRename(ns, options)'; Replace = ''; Tests = 'NamespaceNamingTests.NamespacesOfPublicTypes_AreLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '|| !PublicApi.CanRename(parameter, '; Replace = '|| false && !PublicApi.CanRename(parameter, '; Tests = 'CamelCaseNamingTests.PublicApiParameters_AreLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = 'if ((!renamePublicApi && PublicApi.IsVisible(parameter))'; Replace = 'if (false'; Tests = 'ParameterMatchesBaseTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '(renamePublicApi || !PublicApi.IsVisible(baseParameter))'; Replace = 'true'; Tests = 'ParameterMatchesBaseTests.PublicApi_IsLeftAloneByDefault' }
        @{ File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'; Find = '|| (!PublicApi.IsRenameAllowed(options) && TupleElementNames.IsInPublicSignature('; Replace = '|| (false && TupleElementNames.IsInPublicSignature('; Tests = 'TupleElementNamingTests.PublicSignatures_AreLeftAloneByDefault' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = '|| !TupleElementNames.IsInPublicSignature(element, model, cancellationToken)))'; Replace = '|| true))'; Tests = 'TupleElementNamingTests.PublicSignatures_AreLeftAloneByDefault' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'if (!renamePublicApi && symbols.Any(PublicApi.IsVisible))'; Replace = 'if (false)'; Tests = 'PascalCaseNamingTests.APublicImplementation_KeepsTheName' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '|| (!renamePublicApi && PublicApi.IsVisible(current))'; Replace = ''; Tests = 'CamelCaseNamingTests.PublicApiParameters_AreLeftAloneByDefault' }

        # Names in disabled #if code (DisabledCode), SCREAMING_CASE locals joined word by word
        @{ File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'; Find = '&& !DisabledCode.Mentions(context.Compilation, symbol.Name))'; Replace = ')'; Tests = 'PrefixNamingTests.NamesInDisabledCode_AreSkipped' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'; Find = '&& !DisabledCode.Mentions(symbol.ContainingSymbol, symbol.Name, context.CancellationToken))'; Replace = ')'; Tests = 'PrefixNamingTests.NamesInDisabledCode_AreSkipped' }
        @{ File = 'src/StyleBro.Analyzers/Naming/DisabledCode.cs'; Find = '&& declaration.FullSpan.Contains(trivia.Span)'; Replace = ''; Tests = 'PrefixNamingTests.DisabledCodeWithoutTheName_DoesNotBlock' }
        @{ File = 'src/StyleBro.Analyzers/Naming/DisabledCode.cs'; Find = ' && (end == text.Length || !IsIdentifierChar(text[end]))'; Replace = ''; Tests = 'PrefixNamingTests.DisabledCodeWithoutTheName_DoesNotBlock' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '|| (symbol is not IMethodSymbol { MethodKind: MethodKind.LocalFunction } && DisabledCode.Mentions(context.Compilation, symbol.Name)))'; Replace = ')'; Tests = 'PascalCaseNamingTests.NamesInDisabledCode_AreSkipped' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'; Find = '&& (field.DeclaredAccessibility == Accessibility.Private || !DisabledCode.Mentions(context.Compilation, field.Name))'; Replace = ''; Tests = 'FieldNamingTests.NonPrivateFieldsInDisabledCode_AreSkipped' }
        @{ File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'; Find = '|| DisabledCode.Mentions(context.Compilation, name)'; Replace = ''; Tests = 'TupleElementNamingTests.NamesInDisabledCode_AreSkipped' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = '? FieldNames.GetJoinedName(core, FieldNames.FieldCasing.Camel) ?? ToCamelCase(name)'; Replace = '? ToCamelCase(name)'; Tests = 'CamelCaseNamingTests.NewName' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'several && IsAllUpper(first) ? first.ToLowerInvariant() : '; Replace = ''; Tests = 'CamelCaseNamingTests.NewName' }

        # Kept findings (2026-10-10): a rename the fix keeps on purpose offers no code action
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseNamingCodeFixProvider.cs'; Find = '|| await CamelCaseRenamer.GetKeptReasonAsync(context.Document.Project.Solution, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false) is not null)'; Replace = ')'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementNamingCodeFixProvider.cs'; Find = '&& await TupleElementRenamer.GetKeptReasonAsync(context.Document.Project.Solution, rename.OldName, rename.NewName, context.CancellationToken).ConfigureAwait(false) is null)'; Replace = ')'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'MatchInStrings(symbol, names.Strings) is { } match ? (KeptReason.NameInString'; Replace = 'MatchInStrings(symbol, names.Strings) is { } match && false ? (KeptReason.NameInString'; Tests = 'KeptFindingsTests' }
    )
}
