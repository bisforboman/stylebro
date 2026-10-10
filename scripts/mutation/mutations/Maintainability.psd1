# Mutations for Maintainability (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # BRO1408 (redundant base type)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'; Find = 'EnumUnderlyingType.SpecialType: SpecialType.System_Int32'; Replace = 'EnumUnderlyingType: not null'; Tests = 'RedundantBaseTypeTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'; Find = '.Type?.SpecialType == SpecialType.System_Object'; Replace = '.Type is not null'; Tests = 'RedundantBaseTypeTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'; Find = 'return Trivia.IsBlank(declaration, span) ?'; Replace = 'return true ?'; Tests = 'RedundantBaseTypeTests' }

        # BRO1404/BRO1007 (access modifiers)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'; Find = 'if (!preference.Required'; Replace = 'if (false'; Tests = 'AccessModifiersTests' }

        # BRO1405-BRO1407 (parentheses)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = '!node.IsPartOfStructuredTrivia() && '; Replace = ''; Tests = 'ParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = ' && ParsesTheSame(new[] { node }, text)'; Replace = ''; Tests = 'ParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = 'ParsesTheSame(accepted.Append(node).ToList(), text)'; Replace = 'true'; Tests = 'ParenthesesTests' }

        # BRO1410 (pattern parentheses) and the fading companions BRO1405_p/BRO1410_p (2026-10-09)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = '!(DeclaresPrecedence(pattern) && keepPrecedence()) && '; Replace = ''; Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreLeftToBro1407' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = ' && ParsesTheSame(new[] { pattern }, text)'; Replace = ''; Tests = 'PatternParenthesesTests.ParenthesesThatChangeTheMeaning_AreKept' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = '&& !outer.IsKind(binary.Kind())'; Replace = ''; Tests = 'PatternParenthesesTests.UnnecessaryPatternParentheses_AreRemoved' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = 'inner = nested.Pattern;'; Replace = 'break;'; Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreLeftToBro1407' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/ParenthesesAnalyzer.cs'; Find = 'Severities.IsOn(c.Compilation.Options, tree, DiagnosticIds.ConditionalPrecedence, c.CancellationToken)'; Replace = 'true'; Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenBro1407IsOff' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/ParenthesesAnalyzer.cs'; Find = '&& Precedence.IsWanted(DiagnosticIds.ConditionalPrecedence, c.Options.AnalyzerConfigOptionsProvider.GetOptions(tree))'; Replace = ''; Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenTheSdkSettingSaysNever' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/ParenthesesAnalyzer.cs'; Find = 'if (Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, rule.Id, c.CancellationToken))'; Replace = 'if (true)'; Tests = 'PatternParenthesesTests.FadedParentheses_AreNotReported_WhenTheRuleIsOff' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/ParenthesesCodeFixProvider.cs'; Find = 'Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.ConditionalPrecedence, cancellationToken)'; Replace = 'true'; Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenBro1407IsOff' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/ParenthesesCodeFixProvider.cs'; Find = '&& Precedence.IsWanted(DiagnosticIds.ConditionalPrecedence, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree))'; Replace = ''; Tests = 'PatternParenthesesTests.ParenthesesAroundAndInsideOr_AreRemoved_WhenTheSdkSettingSaysNever' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'; Find = ' || node.IsPartOfStructuredTrivia()'; Replace = ''; Tests = 'PrecedenceTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/PrecedenceAnalyzer.cs'; Find = 'if (!Precedence.IsWanted(id, options))'; Replace = 'if (false)'; Tests = 'PrecedenceTests' }

        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'; Find = ' || symbol is INamedTypeSymbol { IsFileLocal: true })'; Replace = ')'; Tests = 'AccessModifiersTests' }

        # BRO1401 (trailing commas): stylebro_trailing_comma = omit
        @{ File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'; Find = '|| Omits(getOptions()) != hasComma'; Replace = '|| !hasComma'; Tests = 'TrailingCommaTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'; Find = '|| HasDirectiveBetween(node, l.OpenBrace, l.CloseBrace)'; Replace = ''; Tests = 'TrailingCommaTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'; Find = 'while (start > 0 && text[start - 1] is'; Replace = 'while (false && text[start - 1] is'; Tests = 'TrailingCommaTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'; Find = "text[end] == '/' ?"; Replace = 'false ?'; Tests = 'TrailingCommaTests' }

        # BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'case SyntaxKind.OverrideKeyword:'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'case SyntaxKind.VirtualKeyword:'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'case SyntaxKind.AbstractKeyword:'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'case SyntaxKind.ExternKeyword:'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'case SyntaxKind.PartialKeyword:'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| method.AttributeLists.Count > 0'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| method.Parent is not TypeDeclarationSyntax or InterfaceDeclarationSyntax'; Replace = '|| method.Parent is not TypeDeclarationSyntax'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| ConventionNames.Contains(method.Identifier.ValueText)'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| HasConditionalDirectives(method)'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'if ((type.BaseList is { } list && HasConditionalDirectives(list)) || '; Replace = 'if ('; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = ' || type.OpenBraceToken.LeadingTrivia.Any(IsConditional))'; Replace = ')'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'node.ContainsDirectives && node.DescendantTrivia().Any(IsConditional)'; Replace = 'node.ContainsDirectives'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| type.Modifiers.Any(SyntaxKind.PrivateKeyword))'; Replace = ')'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| IsVisibleOutside(type)'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'if (current.GetAttributes().Length > 0)'; Replace = 'if (false)'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = 'if (!current.Locations.Any(l => l.IsInSource))'; Replace = 'if (false)'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '|| ImplementsInterfaceMember(symbol, model.Compilation, cancellationToken))'; Replace = ')'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '? derived.Prepend(type)'; Replace = '? new[] { type }'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '                        pending.Push(child);'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/InternalTypeMethods.cs'; Find = '                    pending.Push(type);'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = '|| strings.Contains(method.Identifier.ValueText)'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = 'if (!Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.MemberOrdering, cancellationToken))'; Replace = 'if (true)'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = '.Where(s => changes.Any(c => s.Span.Contains(c.Span)))'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = 'return derived.Any(d => d.Locations.Any(l => l.IsInSource && project.GetDocument(l.SourceTree) is null));'; Replace = 'return false;'; Tests = 'InternalTypeMethodTests' }

        # Generated code and C# versions (real-world: eShopOnWeb's Razor pages, Mapperly's generated accessors, LibGit2Sharp's net472)
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/AccessModifiersCodeFixProvider.cs'; Find = 'preference, version)'; Replace = 'preference)'; Tests = 'AccessModifiersTests' }
    )
}
