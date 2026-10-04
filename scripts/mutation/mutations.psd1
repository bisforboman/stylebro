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
        @{ File = 'src/StyleBro.CodeFixes/Layout/BracesCodeFixProvider.cs'; Find = '.Where(c => Braces.GetChanges(new[] { c }, text, options) is not null)'; Replace = ''; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = '&& Line(text, ifStatement.IfKeyword.SpanStart) == Line(text, child.Span.End)'; Replace = ''; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'child is ReturnStatementSyntax or ThrowStatementSyntax'; Replace = 'child is StatementSyntax or ThrowStatementSyntax'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'if (allowed is not null && reported'; Replace = 'if (false'; Tests = 'BracesTests' }

        # Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'if (!IsOn(DiagnosticIds.MemberOrdering))'; Replace = 'if (false)'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'if (IsOn(DiagnosticIds.BlankLineAfterComment))'; Replace = 'if (true)'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/Regions.cs'; Find = 'RegionDirectiveTriviaSyntax { IsActive: true } region'; Replace = 'RegionDirectiveTriviaSyntax region'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/Regions.cs'; Find = 'if (beforeText.Length == 0 && after + 1 < text.Lines.Count'; Replace = 'if (false && after + 1 < text.Lines.Count'; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'if (IsOn(DiagnosticIds.BlankLineBeforeComment))'; Replace = 'if (false)'; Tests = 'FixOrderTests' }

        # BRO1133 (null check style)
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '(not ? LanguageVersion.CSharp9 : LanguageVersion.CSharp7)'; Replace = '(LanguageVersion.CSharp7)'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '(not ? LanguageVersion.CSharp9 : LanguageVersion.CSharp7)'; Replace = '(not ? LanguageVersion.CSharp9 : LanguageVersion.CSharp1)'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '|| !IsSimpleOperand(operand)'; Replace = ''; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '|| IsInExpressionTree(binary, model, cancellationToken)'; Replace = ''; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '!trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia)'; Replace = 'false'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = 'if (constant?.Expression.IsKind(SyntaxKind.NullLiteralExpression) != true'; Replace = 'if (constant is null'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '|| !IsLooseParent(isPattern)'; Replace = ''; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '&& !IsSingleSpace(isPattern.IsKeyword, unary.OperatorToken)'; Replace = '&& false'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = 'type.TypeKind == TypeKind.Dynamic'; Replace = 'false'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '|| (type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T))'; Replace = ')'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = 'return op.MethodKind == MethodKind.BuiltinOperator'; Replace = 'return true'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '&& op.Parameters[0].Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T'; Replace = ''; Tests = 'NullCheckTests' }

        @{ File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'; Find = '&& symbol is IMethodSymbol { MethodKind: not MethodKind.BuiltinOperator }'; Replace = '&& false'; Tests = 'NullCheckTests' }

        # BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'if (token.IsMissing || previous.IsMissing || next.IsMissing'; Replace = 'if (false'; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'next.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.OpenBracketToken'; Replace = 'next.Kind() is SyntaxKind.OpenBracketToken'; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'next.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.OpenBracketToken'; Replace = 'next.Kind() is SyntaxKind.OpenBraceToken'; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = '|| next.Parent is FromClauseSyntax { Parent: QueryExpressionSyntax }'; Replace = ''; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'if (!before.Concat(after).All('; Replace = 'if (false && !before.Concat(after).All('; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'if (breakBefore == breakAfter || '; Replace = 'if ('; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = ' when !arrow.ArrowToken.GetPreviousToken().Parent!.AncestorsAndSelf().Any(a => a is TypeParameterConstraintClauseSyntax)'; Replace = ''; Tests = 'WrappingPlacementTests' }

        # BRO1603 (periods): closing punctuation, entities, excluded tags; BRO1606: a summary starting with <para>
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '.TrimEnd().TrimEnd(ClosingPunctuation)'; Replace = '.TrimEnd()'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 't.Kind() is SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlEntityLiteralToken'; Replace = 't.IsKind(SyntaxKind.XmlTextLiteralToken)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '&& !excluded.Contains(name) && '; Replace = '&& '; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = ' || excluded.Contains(name) ? null'; Replace = ' ? null'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'GetFirstParagraph(summary) ?? summary'; Replace = 'summary'; Tests = 'DocumentationTests' }

        # BRO1510: an attribute line doesn't make an accessor multi-line
        @{ File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'; Find = '(accessor.Modifiers.Count > 0 ? accessor.Modifiers[0] : accessor.Keyword).SpanStart'; Replace = 'accessor.SpanStart'; Tests = 'AccessorLayoutTests' }

        # BRO1505: a field below a field that spans several lines; BRO1001's sort and BRO1114's split add that blank line
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '=> IsMultiLineField(field, text),'; Replace = '=> false,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'field.AttributeLists.Count > 0 ? field.AttributeLists.Last().FullSpan.End : field.SpanStart'; Replace = 'field.SpanStart'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '|| Layout.ElementSeparation.IsMultiLineField(field, field.SyntaxTree.GetText())'; Replace = ''; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = ' || text.ToString(variables[i - 1].Span).IndexOf(''\n'') >= 0'; Replace = ''; Tests = 'FixOrderTests' }

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
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(previous.IsKind(SyntaxKind.OpenBracketToken) && previous.Parent.IsKind(SyntaxKind.CollectionExpression))'; Replace = ''; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(embeddedCommentsOn && Readability.EmbeddedComments.IsMoved(comment, text))'; Replace = ''; Tests = 'BlankLineAfterTests' }

        # BRO1131 (base calls). Not mutated: Speculation.SymbolAfterReplacing vs a position-based lookup of a DETACHED
        # node (equivalent; the original bug passed the attached access.Name, which binds as a member-access name).
        @{ File = 'src/StyleBro.Analyzers/Readability/BaseCalls.cs'; Find = 'var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;'; Replace = 'var dispatchesVirtually = false;'; Tests = 'BaseCallsTests' }

        # BRO1312 (namespace names): the analyzer's skips, then the rename's
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| allowed.Contains(oldName)'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = 'if (parent.GetMembers(newName).Any()'; Replace = 'if (false'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| !NamespaceNames.IsOnlyFrom(ns, a => SymbolEqualityComparer.Default.Equals(a, context.Compilation.Assembly))'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| IsInRootNamespace(fullName, rootNamespace)'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'; Find = '|| declared.Locations.Any(l => l.SourceTree is { } tree && NamespaceNames.IsGenerated(tree)))'; Replace = ')'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'is { } text && fullNameInText.IsMatch(text.ToString()))'; Replace = 'is { } text && false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if ((NamespaceNames.Find(global, oldFullName) is { } ns && !NamespaceNames.IsOnlyFrom(ns, a => assemblies.Contains(a.Name)))'; Replace = 'if (false'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = '|| (parentName.Length == 0 ? global : NamespaceNames.Find(global, parentName))?.GetMembers(newPart).Any() == true)'; Replace = ')'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = '&& fullNameInText.IsMatch(t.ValueText))'; Replace = '&& false)'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = '|| root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && partInText.IsMatch(t.ToString())))'; Replace = ')'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = '|| generated'; Replace = ''; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'if ((!generated && root'; Replace = 'if ((root'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = '|| (IsLookedUp(name) && !model.LookupSymbols(name.SpanStart, name: newPart).IsEmpty))'; Replace = ')'; Tests = 'NamespaceNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'; Find = 'else if (text == newPart && IsLookedUp(name) && SeesMembersOf(model, name, parentName)'; Replace = 'else if (false'; Tests = 'NamespaceNamingTests' }

    )
}
