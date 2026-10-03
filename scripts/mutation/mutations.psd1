# Mutations for scripts/mutation/Invoke-Mutations.ps1: each breaks one guard on purpose (Find must occur exactly once in
# File; Replace takes its place), and at least one test in Tests (a test class filter) must then fail. A mutation that
# survives is a guard no test covers. Add one for every new guard.
@{
    Mutations = @(
        # BRO1514-BRO1516 (braces)
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'statement.ContainsDirectives || parent.ContainsDirectives'; Replace = 'false'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'if (statement.DescendantTokens().Any('; Replace = 'if (false && statement.DescendantTokens().Any('; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'if (preference == BracePreference.Never)'; Replace = 'if (false)'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'preference == BracePreference.WhenMultiline ? IsMultiLineForSdk(child, text) : IsMultiLine(child, text)'; Replace = 'IsMultiLine(child, text)'; Tests = 'BracesTests' }

        # BRO1404/BRO1007 (access modifiers)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'; Find = 'if (!preference.Required'; Replace = 'if (false'; Tests = 'AccessModifiersTests' }

        # BRO1405-BRO1407 (parentheses)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = '!node.IsPartOfStructuredTrivia() && '; Replace = ''; Tests = 'ParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = ' && ParsesTheSame(new[] { node }, text)'; Replace = ''; Tests = 'ParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'; Find = 'ParsesTheSame(accepted.Append(node).ToList(), text)'; Replace = 'true'; Tests = 'ParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'; Find = ' || node.IsPartOfStructuredTrivia()'; Replace = ''; Tests = 'PrecedenceTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/PrecedenceAnalyzer.cs'; Find = 'if (!Precedence.IsWanted(id, options))'; Replace = 'if (false)'; Tests = 'PrecedenceTests' }

        # BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 2)'; Replace = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 0)'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'trivia[start - 2].IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'stop.IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'brace.RawKind != 0 && isOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = ' || owner is DoStatementSyntax or BlockSyntax { Parent: DoStatementSyntax }'; Replace = ''; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '(gapIsReplaced && t.IsKind(SyntaxKind.EndOfLineTrivia))'; Replace = 'false'; Tests = 'SingleLineBlocksTests' }

        # BRO1303/BRO1306/BRO1307 (fields)
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'var newName = isProtected && field.IsReadOnly ?'; Replace = 'var newName = false ?'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '&& field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;'; Replace = '&& field.DeclaredAccessibility != Accessibility.Private;'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '|| !string.IsNullOrEmpty(Get($"dotnet_naming_symbols.{symbols}.required_modifiers"))'; Replace = ''; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'; Find = '&& !(rule == FieldRule.Prefix && FieldNames.IsPrefixRequiredByNamingRule(field.Name.Substring(0, 2), options))'; Replace = ''; Tests = 'FieldNamingTests' }

        # BRO1310 (Hungarian prefixes)
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '|| allowed.Contains(match.Groups["prefix"].Value)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '&& !char.IsUpper(name[0])'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '&& !HungarianNames.IsInNativeMethods(context.Node)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = ', name => HungarianNames.GetVariableName(name, hungarian))'; Replace = ')'; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '|| HungarianNames.IsInNativeMethods(field)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'if (GetNewName(f, key.Item1, hungarian) is { } name)'; Replace = 'if (GetNewName(f, key.Item1) is { } name)'; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'if (hungarian is null || IsPascalChecked(field) ||'; Replace = 'if (hungarian is null ||'; Tests = 'HungarianNamingTests' }

        # BRO1001: order the runtime sees
        @{ File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'; Find = 'var layout = type is StructDeclarationSyntax || type.IsKind(SyntaxKind.RecordStructDeclaration) || HasAttribute(type.AttributeLists, "StructLayout");'; Replace = 'var layout = false;'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'; Find = 'var serialized = HasSerializerAttribute(type.AttributeLists) || members.Any(m => HasSerializerAttribute(m.AttributeLists));'; Replace = 'var serialized = false;'; Tests = 'MemberOrderingTests' }

        # BRO1001 at the namespace level
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (container is CompilationUnitSyntax && members[0].GetFirstToken().GetPreviousToken().IsKind(SyntaxKind.None)'; Replace = 'if (false'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'partialAccess?[i] ?? '; Replace = ''; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'else if (slot == count - 1 && !EndsWithNewLine(members[count - 1]) && EndsWithNewLine(member))'; Replace = 'else if (false)'; Tests = 'MemberOrderingTests' }

        # BRO1131 (base calls). Not mutated: Speculation.SymbolAfterReplacing vs a position-based lookup of a DETACHED
        # node (equivalent; the original bug passed the attached access.Name, which binds as a member-access name).
        @{ File = 'src/StyleBro.Analyzers/Readability/BaseCalls.cs'; Find = 'var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;'; Replace = 'var dispatchesVirtually = false;'; Tests = 'BaseCallsTests' }

    )
}
