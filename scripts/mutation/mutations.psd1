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

        # BRO1302: constructor parameters that name a member
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '            || NamesAMember(parameter)'; Replace = ''; Tests = 'CamelCaseNamingTests' }

        # BRO1311 (tuple element casing)
        @{ File = 'src/StyleBro.Analyzers/Naming/TupleElementNamingAnalyzer.cs'; Find = '|| TupleElementNames.InheritsNames(element, context.SemanticModel, context.CancellationToken)'; Replace = ''; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = 'if (renamed is not null && !await AddsErrorsAsync(solution, renamed, cancellationToken).ConfigureAwait(false))'; Replace = 'if (renamed is not null)'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/TupleElementNames.cs'; Find = '&& HasTupleNames(b.OriginalDefinition)'; Replace = ''; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = 'if (InferredNameStart(name) is { } start)'; Replace = 'if (InferredNameStart(name) is { } start && false)'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = 'if (target is not null && target.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))'; Replace = 'if (true)'; Tests = 'TupleElementNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'; Find = '&& element.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))'; Replace = ')'; Tests = 'TupleElementNamingTests' }

        # BRO1310 (Hungarian prefixes)
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '|| allowed.Contains(match.Groups["prefix"].Value)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '&& !char.IsUpper(name[0])'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '&& !HungarianNames.IsInNativeMethods(context.Node)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = ', name => HungarianNames.GetVariableName(name, hungarian))'; Replace = ')'; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = '|| HungarianNames.IsInNativeMethods(field)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'if (GetNewName(f, key.Item1, hungarian) is { } name)'; Replace = 'if (GetNewName(f, key.Item1) is { } name)'; Tests = 'HungarianNamingTests' }

        # BRO1001 inside regions
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (kind is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia'; Replace = 'if (false'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '|| kind is SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia)'; Replace = ')'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (i > 0 && LastDirective(trivia) >= 0)'; Replace = 'if (false)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'var split = LastDirective(trivia) + 1;'; Replace = 'var split = 0;'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (EndsWithDirective(layout) && StartsWithDocComment(split[source].Content) && !StartsWithDocComment(split[slot].Content))'; Replace = 'if (false)'; Tests = 'MemberOrderingTests' }

        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (createsSeparation || createsComment || createsDoc)'; Replace = 'if (createsSeparation || createsComment)'; Tests = 'MemberOrderingTests' }

        # BRO1001: order the runtime sees
        @{ File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'; Find = 'var layout = type is StructDeclarationSyntax || type.IsKind(SyntaxKind.RecordStructDeclaration) || HasAttribute(type.AttributeLists, "StructLayout");'; Replace = 'var layout = false;'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'; Find = 'var serialized = HasSerializerAttribute(type.AttributeLists) || members.Any(m => HasSerializerAttribute(m.AttributeLists));'; Replace = 'var serialized = false;'; Tests = 'MemberOrderingTests' }

        # BRO1001 at the namespace level
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (container is CompilationUnitSyntax && members[0].GetFirstToken().GetPreviousToken().IsKind(SyntaxKind.None)'; Replace = 'if (false'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'partialAccess?[i] ?? '; Replace = ''; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'else if (slot == count - 1 && !EndsWithNewLine(members[count - 1]) && EndsWithNewLine(member))'; Replace = 'else if (false)'; Tests = 'MemberOrderingTests' }

        # BRO1132 (embedded comments)
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| Line(text, owner.GetFirstToken().SpanStart) != Line(text, previous.SpanStart)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| gap.Any(t => t.IsDirective)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| comments.Any(c => Line(text, c.Span.Start) != Line(text, c.Span.End))'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| Line(text, openBrace.GetNextToken().SpanStart) == Line(text, openBrace.SpanStart)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '&& !trivia.ToString().StartsWith("////", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'when block.Parent is not null && BlockOwners.Any(block.Parent.IsKind)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'next.IsKind(SyntaxKind.CloseBraceToken)'; Replace = 'false'; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(embeddedCommentsOn && Readability.EmbeddedComments.IsMoved(comment, text))'; Replace = ''; Tests = 'BlankLineAfterTests' }

        # BRO1131 (base calls). Not mutated: Speculation.SymbolAfterReplacing vs a position-based lookup of a DETACHED
        # node (equivalent; the original bug passed the attached access.Name, which binds as a member-access name).
        @{ File = 'src/StyleBro.Analyzers/Readability/BaseCalls.cs'; Find = 'var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;'; Replace = 'var dispatchesVirtually = false;'; Tests = 'BaseCallsTests' }

    )
}
