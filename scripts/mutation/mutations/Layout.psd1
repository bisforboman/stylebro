# Mutations for Layout (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # BRO1527 (auto-accessors on one line)
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = '|| list.Accessors.Count == 0 '; Replace = ''; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'if (Line(text, before.Span.End) == Line(text, close.SpanStart)'; Replace = 'if (false'; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'preserve.Trim().Equals("false", StringComparison.OrdinalIgnoreCase)'; Replace = 'false'; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'a.Body is not null || '; Replace = ''; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'a.ExpressionBody is not null || '; Replace = ''; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'a.AttributeLists.Count > 0 || '; Replace = ''; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = ' || Line(text, a.SpanStart) != Line(text, a.Span.End))'; Replace = ')'; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = '&& Line(text, initializer.SpanStart) != Line(text, close.SpanStart)'; Replace = '&& false'; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'if (list.Accessors.Any(a => !Trivia.IsBlank(a, a.Span)))'; Replace = 'if (false)'; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'if (!gaps.All(gap => SingleLineBlocks.Gap(gap.Before, gap.After, " ", text, changes)))'; Replace = 'if (!gaps.All(gap => SingleLineBlocks.Gap(gap.Before, gap.After, " ", text, changes) || true))'; Tests = 'AutoAccessorLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = '&& Line(text, start) == Line(text, list.OpenBraceToken.GetPreviousToken().Span.End)'; Replace = ''; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = '&& Line(text, list.CloseBraceToken.SpanStart) == Line(text, property.Span.End)'; Replace = ''; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'var start = property.AttributeLists.Count > 0 ? property.AttributeLists.Last().FullSpan.End : property.SpanStart;'; Replace = 'var start = property.SpanStart;'; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '!(autoAccessorLines is not null && '; Replace = '!(false && '; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparationAnalyzer.cs'; Find = 'var autoAccessorLines = Severities.IsOn(compilationOptions, c.Tree, DiagnosticIds.AutoAccessorsOnOneLine, c.CancellationToken)'; Replace = 'var autoAccessorLines = true'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.CodeFixes/Layout/ElementSeparationCodeFixProvider.cs'; Find = 'DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken) ? options : null;'; Replace = 'DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken) ? options : options;'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'if (BlankLineRuns.JudgesGapAfter(close, next, text, isOn, gapIsReplaced: false))'; Replace = 'if (false)'; Tests = 'AutoAccessorLinesTests' }

        # BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = 'if (exemptPrefixes.Any(p => commentText.StartsWith(p, StringComparison.Ordinal)))'; Replace = 'if (false)'; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = 'commentText.StartsWith(p, StringComparison.Ordinal)'; Replace = 'commentText.StartsWith(p, StringComparison.OrdinalIgnoreCase)'; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '.Where(p => p.Length > 0)'; Replace = ''; Tests = 'BlankLineAfterTests' }

        # BRO1514-BRO1516 (braces)
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'statement.ContainsDirectives || parent.ContainsDirectives'; Replace = 'false'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'if (statement.DescendantTokens().Any('; Replace = 'if (false && statement.DescendantTokens().Any('; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'if (preference == BracePreference.Never)'; Replace = 'if (false)'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'preference == BracePreference.WhenMultiline ? IsMultiLineForSdk(child, text) : IsMultiLine(child, text)'; Replace = 'IsMultiLine(child, text)'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.CodeFixes/Layout/BracesCodeFixProvider.cs'; Find = '.Where(c => Braces.GetChanges(new[] { c }, text, options) is not null)'; Replace = ''; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = '&& Line(text, ifStatement.IfKeyword.SpanStart) == Line(text, child.Span.End)'; Replace = ''; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'child is ReturnStatementSyntax or ThrowStatementSyntax'; Replace = 'child is StatementSyntax or ThrowStatementSyntax'; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = 'if (allowed is not null && reported'; Replace = 'if (false'; Tests = 'BracesTests' }

        # BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'if (token.IsMissing || previous.IsMissing || next.IsMissing'; Replace = 'if (false'; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'next.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.OpenBracketToken'; Replace = 'next.Kind() is SyntaxKind.OpenBracketToken'; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'next.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.OpenBracketToken'; Replace = 'next.Kind() is SyntaxKind.OpenBraceToken'; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = '|| next.Parent is FromClauseSyntax { Parent: QueryExpressionSyntax }'; Replace = ''; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'if (!before.Concat(after).All('; Replace = 'if (false && !before.Concat(after).All('; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = 'if (breakBefore == breakAfter || '; Replace = 'if ('; Tests = 'WrappingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'; Find = ' when !arrow.ArrowToken.GetPreviousToken().Parent!.AncestorsAndSelf().Any(a => a is TypeParameterConstraintClauseSyntax)'; Replace = ''; Tests = 'WrappingPlacementTests' }

        # BRO1523 (call chain layout)
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = 'if (!starts.Contains(true) || '; Replace = 'if ('; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = 'if (!directive.IsKind(SyntaxKind.RegionDirectiveTrivia) && '; Replace = 'if (false && '; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '!directive.IsKind(SyntaxKind.RegionDirectiveTrivia) && !'; Replace = '!'; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '&& !directive.IsKind(SyntaxKind.EndRegionDirectiveTrivia)'; Replace = ''; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '.Where(n => !regionLines.Contains(n))'; Replace = ''; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '|| chain.ContainsDiagnostics'; Replace = '|| false'; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '|| chain.Ancestors().Any(a => a is InterpolationSyntax)'; Replace = ''; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '|| !links[i - 1].Call '; Replace = ''; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '|| line == firstLine '; Replace = ''; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '|| !links.Skip(i).Any(l => l.Call)'; Replace = ''; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = 'if (previous.TrailingTrivia.Concat(token.LeadingTrivia).Any('; Replace = 'if (false && previous.TrailingTrivia.Concat(token.LeadingTrivia).Any('; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = 'if (Enumerable.Range(line + 1, Line(text, end) - line)'; Replace = 'if (false && Enumerable.Range(line + 1, Line(text, end) - line)'; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = '.Any(l => l.Trim().Length > 0 && '; Replace = '.Any(l => '; Tests = 'CallChainTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/CallChains.cs'; Find = 'ConditionalAccessExpressionSyntax => true,'; Replace = 'ConditionalAccessExpressionSyntax => false,'; Tests = 'CallChainTests' }

        # BRO1525 (no blank line after attributes)
        @{ File = 'src/StyleBro.Analyzers/Layout/AttributeBlankLines.cs'; Find = 'if (list.Parent is null or CompilationUnitSyntax || '; Replace = 'if ('; Tests = 'AttributeBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AttributeBlankLines.cs'; Find = ' || list.Parent.ContainsDiagnostics)'; Replace = ')'; Tests = 'AttributeBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AttributeBlankLines.cs'; Find = 'if (!close.TrailingTrivia.Concat('; Replace = 'if (false && !close.TrailingTrivia.Concat('; Tests = 'AttributeBlankLinesTests' }

        # BRO1526 (blank line between switch sections)
        @{ File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'; Find = 'if (node.Sections.Count < 2 || node.ContainsDiagnostics)'; Replace = 'if (node.Sections.Count < 2)'; Tests = 'SwitchSectionBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'; Find = 'if (!previous.TrailingTrivia.LastOrDefault().IsKind(SyntaxKind.EndOfLineTrivia)'; Replace = 'if (false'; Tests = 'SwitchSectionBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'; Find = '|| !next.LeadingTrivia.All('; Replace = '|| false && !next.LeadingTrivia.All('; Tests = 'SwitchSectionBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'; Find = '|| (previous.IsKind(SyntaxKind.CloseBraceToken) && BlankLineRuns.JudgesGapAfter('; Replace = '|| (false && BlankLineRuns.JudgesGapAfter('; Tests = 'SwitchSectionBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SwitchSectionBlankLines.cs'; Find = ' && node.Sections[i - 1].Statements.Last() is not BlockSyntax'; Replace = ''; Tests = 'SwitchSectionBlankLinesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '&& OpeningLine(brace, text) != text.Lines.GetLineFromPosition(brace.SpanStart).LineNumber;'; Replace = ';'; Tests = 'SwitchSectionBlankLinesTests' }

        # BRO1524 (split conditional expressions)
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (conditional.ContainsDiagnostics'; Replace = 'if (false'; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = '|| conditional.WhenFalse is ConditionalExpressionSyntax'; Replace = ''; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = '|| (conditional.Parent is ConditionalExpressionSyntax parent && parent.WhenFalse == conditional)'; Replace = ''; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (gaps.SelectMany(g => g).Any('; Replace = 'if (false && gaps.SelectMany(g => g).Any('; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (before || after || Line(text, part.SpanStart) != Line(text, part.Span.End))'; Replace = 'if (before || after)'; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (before || after || '; Replace = 'if (after || '; Tests = 'ConditionalLayoutTests' }

        # BRO1510: an attribute line doesn't make an accessor multi-line
        @{ File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'; Find = '(accessor.Modifiers.Count > 0 ? accessor.Modifiers[0] : accessor.Keyword).SpanStart'; Replace = 'accessor.SpanStart'; Tests = 'AccessorLayoutTests' }

        # BRO1505: a field below a field that spans several lines; BRO1001's sort and BRO1114's split add that blank line
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '=> SpansSeveralLines(field, text),'; Replace = '=> false,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'member.AttributeLists.Count > 0 ? member.AttributeLists.Last().FullSpan.End : member.SpanStart'; Replace = 'member.SpanStart'; Tests = 'ElementSeparationTests' }

        # Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '(PropertyDeclarationSyntax property, PropertyDeclarationSyntax next) => IsMultiLineProperty(property, text, autoAccessorLines) || IsMultiLineProperty(next, text, autoAccessorLines)'; Replace = '(PropertyDeclarationSyntax property, PropertyDeclarationSyntax next) => true'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '|| IsMultiLineProperty(next, text, autoAccessorLines)'; Replace = ''; Tests = 'ElementSeparationTests' }

        # BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 2)'; Replace = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 0)'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'trivia[start - 2].IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'stop.IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'brace.RawKind != 0 && isOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = ' || owner is DoStatementSyntax or BlockSyntax { Parent: DoStatementSyntax }'; Replace = ''; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '(gapIsReplaced && t.IsKind(SyntaxKind.EndOfLineTrivia))'; Replace = 'false'; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '(runs ??= []).Add(found);'; Replace = '{ (runs ??= []).Add(found); return runs; }'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '&& TrailingBlankLines.GetBlankLinesAfterComment(trivia[start - 2], text).Count > 0)'; Replace = ')'; Tests = 'BlankLineRunsTests' }

        # BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '!(Readability.EmbeddedComments.GetMovingRule(comment, text) is { } rule && isOn(rule))'; Replace = 'true'; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(previous.IsKind(SyntaxKind.OpenBracketToken) && previous.Parent.IsKind(SyntaxKind.CollectionExpression))'; Replace = ''; Tests = 'BlankLineAfterTests' }

        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !previous.IsKind(SyntaxKind.EqualsGreaterThanToken)'; Replace = ''; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(token.Parent is InitializerExpressionSyntax { Parent: InitializerExpressionSyntax })'; Replace = ''; Tests = 'BlankLineBeforeTests' }

        # Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
        @{ File = 'src/StyleBro.Analyzers/Layout/BracesAnalyzer.cs'; Find = '&& !Braces.IsLeftToExpansion('; Replace = '&& true || !Braces.IsLeftToExpansion('; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = '&& isOn(braces.IsElement ? DiagnosticIds.SingleLineElement : DiagnosticIds.SingleLineStatementBlock)'; Replace = ''; Tests = 'BracesTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = 'Braces.AddToExpansion(block, text, changes, options, isOn);'; Replace = ''; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/Braces.cs'; Find = ' && n.Ancestors().OfType<BlockSyntax>().FirstOrDefault() == newBlock'; Replace = ''; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = 'changes.AddRange(enclosing);'; Replace = ''; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = '&& isOn(braces.IsElement ? DiagnosticIds.SingleLineElement : DiagnosticIds.SingleLineStatementBlock)'; Replace = ''; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = '&& BlankLineRuns.WantsBlankLineAfter(previous.Parent!, previous, items[i].GetFirstToken(), isOn, gapIsReplaced: true)'; Replace = ''; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = 'previous.IsKind(SyntaxKind.CloseBraceToken)'; Replace = 'true'; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = 'if (AllowsEmpty(options))'; Replace = 'if (false)'; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = '&& !TrailingCommas.Omits(options)'; Replace = ''; Tests = 'SingleLineBlocksTests' }

        # Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
        # BRO1105 / BRO1111 same_line
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'out var value) && value.Trim() == "true";'; Replace = 'out var value);'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'if (allowAdjacentSingleLine && IsCompact(previous, text, autoAccessorLines) && IsCompact(current, text, autoAccessorLines))'; Replace = 'if (allowAdjacentSingleLine)'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'MethodDeclarationSyntax { ConstraintClauses.Count: > 0 } => false,'; Replace = ''; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '&& method.Body is null && !SpansSeveralLines(method, text),'; Replace = '&& !SpansSeveralLines(method, text),'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '&& method.Body is null && !SpansSeveralLines(method, text),'; Replace = '&& method.Body is null,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'PropertyDeclarationSyntax property => !HasBlockAccessor(property) && '; Replace = 'PropertyDeclarationSyntax property => '; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'or EventDeclarationSyntax && !HasBlockAccessor(other) && '; Replace = 'or EventDeclarationSyntax && '; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'EventFieldDeclarationSyntax eventField => !SpansSeveralLines(eventField, text),'; Replace = 'EventFieldDeclarationSyntax eventField => true,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparationAnalyzer.cs'; Find = 'var allowAdjacent = ElementSeparation.AllowsAdjacentSingleLineMembers(options);'; Replace = 'var allowAdjacent = false;'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = 'allowAdjacentSingleLine: ElementSeparation.AllowsAdjacentSingleLineMembers(options)'; Replace = 'allowAdjacentSingleLine: false'; Tests = 'SingleLineBlocksTests' }

        # A documented property is set apart (BRO1505), BRO1601 adds the blank line below (Ocelot)
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '|| HasDocumentation(property) || HasDocumentation(next),'; Replace = ','; Tests = 'ElementSeparationTests.DocumentedProperties_AreSetApart' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '|| HasBlankLineBetween(property, next, text))'; Replace = ')'; Tests = 'DocumentationTests.InheritDoc_OnAdjacentProperties_SeparatesThem' }

        # A comment below code (BRO1504, BRO1506, BRO1001; eShop)
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !IsCommentBelowCode(comment, text);'; Replace = ';'; Tests = 'BlankLineAfterTests.ACommentBelowCode_FollowedByABlankLine_IsNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'; Find = '|| BlankLines.IsCommentBelowCode(comment, text))'; Replace = ')'; Tests = 'TrailingBlankLinesTests.ACommentBelowCode_KeepsTheBlankLineBelowIt' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& previous.IsKind(SyntaxKind.SemicolonToken)'; Replace = ''; Tests = 'TrailingBlankLinesTests.ACommentBelowCode_KeepsTheBlankLineBelowIt' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !comment.Token.IsKind(SyntaxKind.CloseBraceToken);'; Replace = ';'; Tests = 'TrailingBlankLinesTests.ACommentBelowCode_KeepsTheBlankLineBelowIt' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& (previous.Parent is StatementSyntax || previous.Parent is MemberDeclarationSyntax and not BaseNamespaceDeclarationSyntax)'; Replace = ''; Tests = 'TrailingBlankLinesTests.BlankLinesAfterComments_AreRemoved' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '(content.StartsWith("#region", StringComparison.Ordinal) || content.StartsWith("#endregion", StringComparison.Ordinal))'; Replace = 'false'; Tests = 'FixOrderTests.Regions_ACommentAfterCodeAboveTheRemovedLines' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& IsBlank(text, text.Lines[below].Span)'; Replace = ''; Tests = 'BlankLineAfterTests.ACommentBelowCode_FollowedByABlankLine_IsNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = 'while (last + 1 < text.Lines.Count && IsComment(last + 1))'; Replace = 'while (false)'; Tests = 'BlankLineAfterTests.ACommentBelowCode_FollowedByABlankLine_IsNotReported' }
    )
}
