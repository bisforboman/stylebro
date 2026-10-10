# Mutations for Readability (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # BRO1136 (lambda parentheses)
        @{ File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'; Find = 'if (lambda.ParameterList.Parameters.Count != 1'; Replace = 'if (lambda.ParameterList.Parameters.Count < 1'; Tests = 'LambdaParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'; Find = 'is not { Type: null } parameter'; Replace = 'is not { } parameter'; Tests = 'LambdaParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'; Find = '|| parameter.AttributeLists.Count > 0'; Replace = ''; Tests = 'LambdaParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'; Find = '|| lambda.AttributeLists.Count > 0'; Replace = ''; Tests = 'LambdaParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'; Find = '|| lambda.ReturnType is not null'; Replace = ''; Tests = 'LambdaParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'; Find = '.Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))'; Replace = '.Any(t => false))'; Tests = 'LambdaParenthesesTests' }

        # BRO1137 (redundant return; / yield break;)
        @{ File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'; Find = '|| block.Statements.Last() != statement'; Replace = ''; Tests = 'RedundantJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'; Find = '|| block.Parent is not (BaseMethodDeclarationSyntax'; Replace = '|| block.Parent is (ArgumentSyntax'; Tests = 'RedundantJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'; Find = '|| statement.DescendantTrivia().Any('; Replace = '|| statement.DescendantTrivia().All('; Tests = 'RedundantJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'; Find = '.OfType<YieldStatementSyntax>().Skip(1).Any()'; Replace = '.OfType<YieldStatementSyntax>().Any()'; Tests = 'RedundantJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'; Find = 'n == block || !IsFunction(n)'; Replace = 'true'; Tests = 'RedundantJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'; Find = 'while (first > 0 &&'; Replace = 'while (false &&'; Tests = 'RedundantJumpTests' }

        # BRO1138 (unneeded $, @, raw string)
        @{ File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'; Find = '.ConvertedType?.SpecialType is SpecialType.System_String or SpecialType.System_Object'; Replace = '.ConvertedType is not null'; Tests = 'StringPrefixTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'; Find = 'if (content.Length == 0)'; Replace = 'if (false)'; Tests = 'StringPrefixTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'; Find = 'content.Length == 0 || content.Any('; Replace = 'content.Any('; Tests = 'StringPrefixTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'; Find = "c is '\\' or '`"' or"; Replace = 'c is'; Tests = 'StringPrefixTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/StringPrefixes.cs'; Find = '? Plain(content, string.Empty) ?? raw : raw;'; Replace = '? raw : raw;'; Tests = 'StringPrefixTests' }

        # BRO1139 (else if)
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = '&& !(isOn(DiagnosticIds.ElseAfterJump) && e.Parent is IfStatementSyntax owner && ElseAfterJump.IsCandidate(owner, text, options, isOn))'; Replace = '&& true'; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = '&& !block.CloseBraceToken.GetNextToken().IsKind(SyntaxKind.ElseKeyword)'; Replace = ''; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = '|| gaps.Any('; Replace = '|| gaps.All('; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = 'if (indent < shift)'; Replace = 'if (false)'; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = 'return shift < 0 ? null :'; Replace = 'return false ? null :'; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = 'if (lineEdits.Count > 0 && parts[0].If.DescendantTokens()'; Replace = 'if (false && parts[0].If.DescendantTokens()'; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = 'if (after.All(f => before.TryGetValue(f.Child.SpanStart, out var id) && id == f.Id))'; Replace = 'if (true)'; Tests = 'ElseIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'; Find = 'if (after.All(f => before.TryGetValue(f.Child.SpanStart, out var id) && id == f.Id))'; Replace = 'if (false)'; Tests = 'ElseIfTests' }

        # BRO1140 (empty record body) and BRO1145 (empty class, struct or interface body)
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = 'record.ParameterList is null ? null : '; Replace = ''; Tests = 'EmptyRecordBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = '|| type.Members.Count > 0'; Replace = ''; Tests = 'EmptyRecordBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = 'return Trivia.IsBlank(type, span) ?'; Replace = 'return true ?'; Tests = 'EmptyRecordBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = '.LanguageVersion >= LanguageVersion.CSharp12 ?'; Replace = '.LanguageVersion >= LanguageVersion.CSharp1 ?'; Tests = 'EmptyTypeBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = 'if (frameworks.Count < 2 || '; Replace = 'if ('; Tests = 'EmptyTypeBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = 'frameworks.All(f => MultiTargetSuppressor.Has(f, (new Version(8, 0), null)))'; Replace = 'false'; Tests = 'EmptyTypeBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = 'return !string.IsNullOrWhiteSpace(langVersion) && '; Replace = 'return '; Tests = 'EmptyTypeBodyTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyRecordBodies.cs'; Find = ' && !string.Equals(langVersion!.Trim(), defaultVersion?.Trim(), StringComparison.OrdinalIgnoreCase)'; Replace = ''; Tests = 'EmptyTypeBodyTests' }

        # BRO1146 (record class)
        @{ File = 'src/StyleBro.Analyzers/Readability/RecordClassKeywords.cs'; Find = 'return Trivia.IsBlank(record, span) ?'; Replace = 'return true ?'; Tests = 'RecordClassKeywordTests' }

        # BRO1149 (nested ifs)
        # Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'outer.Else is not null'; Replace = 'false'; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'inner.Else is not null'; Replace = 'false'; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'outer.ContainsDirectives'; Replace = 'false'; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = '|| !Trivia.IsBlank(outer, TextSpan.FromBounds(outer.Condition.Span.End, inner.Condition.SpanStart))'; Replace = ''; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = '|| !Trivia.IsBlank(outer, TextSpan.FromBounds(inner.Span.End, outer.Span.End))'; Replace = ''; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = '|| !Trivia.IsBlank(deepest, TextSpan.FromBounds(deepest.Condition.Span.End, deepest.CloseParenToken.SpanStart))'; Replace = ''; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = '|| condition.IsKind(SyntaxKind.LogicalOrExpression)'; Replace = ''; Tests = 'NestedIfTests.LooserOperands_GetParentheses' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'if (node.DescendantTokens().Any(t => text.Lines'; Replace = 'if (false && node.DescendantTokens().Any(t => text.Lines'; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'if (children.Count == 0)'; Replace = 'if (true)'; Tests = 'NestedIfTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'IndentOf(text, current.SpanStart) - topIndent'; Replace = '0'; Tests = 'NestedIfTests.MultiLineInnerCondition_MovesLeft' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = 'return scope is not null'; Replace = 'return false && scope is not null'; Tests = 'NestedIfTests.Skipped' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'; Find = '|| outer.GetLastToken().GetNextToken().LeadingTrivia.Any(t => t.IsDirective)'; Replace = ''; Tests = 'NestedIfTests.Skipped' }

        # BRO1150 (Where before Count/Any/...)
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = 'if (GetLinqMethod(model.GetSymbolInfo(where, cancellationToken).Symbol) is null'; Replace = 'if (false'; Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = '|| NullChecks.IsInExpressionTree(terminal, model, cancellationToken)'; Replace = ''; Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = 'if (GetLinqMethod(Speculation.SymbolAfterReplacing(model, terminal, call, cancellationToken)) is null)'; Replace = 'if (false)'; Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = '|| !Trivia.IsBlank(terminal, TextSpan.FromBounds(argument.Span.End, terminal.Span.End))'; Replace = ''; Tests = 'WhereBeforeTerminalTests.OtherCalls_AreNotReported' }

        # BRO1151 (params arrays)
        @{ File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'; Find = 'list.Arguments.Count != method.Parameters.Length'; Replace = 'false'; Tests = 'ParamsArrayTests.OtherArrays_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'; Find = '|| (elements.Count == 1 && model.ClassifyConversion(elements[0], parameter.Type).IsImplicit)'; Replace = ''; Tests = 'ParamsArrayTests.OtherArrays_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'; Find = 'array is CollectionExpressionSyntax ? info.ConvertedType : info.Type'; Replace = 'info.ConvertedType'; Tests = 'ParamsArrayTests.OtherArrays_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'; Find = '|| (elements.Count > 1 && line != text.Lines.GetLineFromPosition(list.Arguments[0].SpanStart).LineNumber)'; Replace = ''; Tests = 'ParamsArrayTests.OtherArrays_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'; Find = 'return SymbolEqualityComparer.Default.Equals(Speculation.SymbolAfterReplacing(model, call, expanded, cancellationToken), method) ? changes : null;'; Replace = 'return changes;'; Tests = 'ParamsArrayTests.OtherArrays_AreNotReported' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParamsArrays.cs'; Find = 'if (removed.Any(span => !Trivia.IsBlank(call, span))'; Replace = 'if (false'; Tests = 'ParamsArrayTests.OtherArrays_AreNotReported' }

        # BRO1141 (object creation parentheses)
        @{ File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'; Find = 'if (creation.Initializer is null || '; Replace = 'if ('; Tests = 'ObjectCreationParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'; Find = '|| arguments.Arguments.Count > 0'; Replace = ''; Tests = 'ObjectCreationParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'; Find = '|| !Trivia.IsBlank(creation, span)'; Replace = ''; Tests = 'ObjectCreationParenthesesTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'; Find = 'return include ? new TextChange'; Replace = 'return false ? new TextChange'; Tests = 'ObjectCreationParenthesesTests' }

        # BRO1142 (one local per declaration)
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = 'declaration.UsingKeyword.IsKind(SyntaxKind.None) ?'; Replace = 'true ?'; Tests = 'CombinedFieldsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = 'if (sameLine && declaration is not LocalDeclarationStatementSyntax)'; Replace = 'if (false)'; Tests = 'CombinedFieldsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = '(declaration is BaseFieldDeclarationSyntax && text.ToString('; Replace = '(true && text.ToString('; Tests = 'CombinedFieldsTests' }

        # BRO1144 (C# 14 contextual keywords; the C# 14 'field' type check is covered by samples/Messy, Roslyn 4.8 can't parse it)
        @{ File = 'src/StyleBro.Analyzers/Readability/ContextualKeywords.cs'; Find = 'MemberAccessExpressionSyntax access => access.Name != name,'; Replace = 'MemberAccessExpressionSyntax access => true,'; Tests = 'ContextualKeywordTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ContextualKeywords.cs'; Find = 'MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => false,'; Replace = 'MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => true,'; Tests = 'ContextualKeywordTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ContextualKeywords.cs'; Find = 'return first == token;'; Replace = 'return true;'; Tests = 'ContextualKeywordTests' }

        # Removing regions also does what BRO1001 and BRO1506 then want, only when they're on
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'if (!IsOn(DiagnosticIds.MemberOrdering))'; Replace = 'if (false)'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'if (IsOn(DiagnosticIds.BlankLineAfterComment))'; Replace = 'if (true)'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'union[union.Count - 1].Span.End >= span.Start'; Replace = 'false'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'while (line.LineNumber > 0 && string.IsNullOrWhiteSpace(line.ToString()))'; Replace = 'while (false)'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/Regions.cs'; Find = 'RegionDirectiveTriviaSyntax { IsActive: true } region'; Replace = 'RegionDirectiveTriviaSyntax region'; Tests = 'RegionsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/Regions.cs'; Find = 'if (beforeText.Length == 0 && after + 1 < text.Lines.Count'; Replace = 'if (false && after + 1 < text.Lines.Count'; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/RegionsCodeFixProvider.cs'; Find = 'if (IsOn(DiagnosticIds.BlankLineBeforeComment))'; Replace = 'if (false)'; Tests = 'FixOrderTests' }

        # BRO1135 (upper-case integer literal suffixes)
        @{ File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'; Find = 'return suffix.Any(char.IsLower) ?'; Replace = 'return true ?'; Tests = 'LiteralSuffixCaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'; Find = 'if (lOnly)'; Replace = 'if (false)'; Tests = 'LiteralSuffixCaseTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'; Find = "return suffix.IndexOf('l') >= 0 ?"; Replace = 'return true ?'; Tests = 'LiteralSuffixCaseTests' }

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

        # BRO1143 (no 'else' after a jump)
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'node.Parent is not BlockSyntax block'; Replace = 'node.FirstAncestorOrSelf<BlockSyntax>() is not { } block'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| node.Else is not { Statement: not IfStatementSyntax } elseClause'; Replace = '|| node.Else is not { } elseClause'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| !EndsInJump(node.Statement)'; Replace = ''; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| node.ContainsDiagnostics'; Replace = ''; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| node.ContainsDirectives'; Replace = ''; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'BlockSyntax { Statements.Count: 0 } => null,'; Replace = ''; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (!OnlyWhitespace(elseClause.ElseKeyword.GetPreviousToken(), body.First.GetFirstToken())'; Replace = 'if (false'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| (elseClause.Statement is BlockSyntax closing && !OnlyWhitespace('; Replace = '|| (elseClause.Statement is BlockSyntax closing && false && !OnlyWhitespace('; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (GetBranchToWrap(node, text, options, isOn) is { } branch && Braces.GetChanges(new[] { branch }, text, options) is null)'; Replace = 'if (false)'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '.Any(f => f.Child == node.Statement)'; Replace = '.Any()'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (ifLine.Start + indent.Length != node.SpanStart)'; Replace = 'if (false)'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (lastLine != firstLine.LineNumber)'; Replace = 'if (false)'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (!text.ToString(line.Span).StartsWith(indent + unit, StringComparison.Ordinal) && '; Replace = 'if (false && '; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (elseClause.Statement.DescendantTokens().Any(t => SpansLines(text, t.Span))'; Replace = 'if (false'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| elseClause.Statement.DescendantTrivia().Any('; Replace = '|| false && elseClause.Statement.DescendantTrivia().Any('; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'if (statements.Any(s => s is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 }))'; Replace = 'if (false)'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '|| !block.DescendantTokens().Any('; Replace = '|| true || !block.DescendantTokens().Any('; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'YieldStatementSyntax yield => yield.IsKind(SyntaxKind.YieldBreakStatement),'; Replace = 'YieldStatementSyntax => true,'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'block.Statements.Count > 0 && EndsInJump(block.Statements.Last()),'; Replace = 'block.Statements.Count > 0,'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = '&& text.Lines.GetLineFromPosition(branch.OpenBraceToken.SpanStart).LineNumber != text.Lines.GetLineFromPosition(branch.CloseBraceToken.SpanStart).LineNumber'; Replace = ''; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'BlankLineRuns.WantsBlankLineAfter(branch, branch.CloseBraceToken, item.First.GetFirstToken(), isOn, gapIsReplaced: true);'; Replace = 'true;'; Tests = 'ElseAfterJumpTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'; Find = 'nested => EndsInJump(nested.Statement) && EndsInJump(inner.Statement),'; Replace = 'nested => EndsInJump(nested.Statement),'; Tests = 'ElseAfterJumpTests' }

        # Following StyleCop master: BRO1104 nint, BRO1604/BRO1605 init, BRO1611 primary constructors, BRO1606 blank summaries
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = '&& typeText is not ("nint" or "nuint"))'; Replace = ')'; Tests = 'DefaultValueConstructorTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = '&& PrefersDefaultLiteral(options)'; Replace = ''; Tests = 'DefaultValueConstructorTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = '>= LanguageVersion.CSharp7_1'; Replace = '>= LanguageVersion.CSharp1'; Tests = 'DefaultValueConstructorTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = '&& SymbolEqualityComparer.Default.Equals(info.Type, info.ConvertedType))'; Replace = ')'; Tests = 'DefaultValueConstructorTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = 'group.AddRange('; Replace = 'new List<ExpressionSyntax>().AddRange('; Tests = 'DefaultValueConstructorTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = '.Where(e => e is DefaultExpressionSyntax'; Replace = '.Where(e => e is null'; Tests = 'DefaultValueConstructorTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'; Find = "&& value.Split(':')[0].Trim().Equals("; Replace = '&& value.Trim().Equals('; Tests = 'DefaultValueConstructorTests' }

        # BRO1106 literal style
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'; Find = '&& !IsInNameof(access)'; Replace = ''; Tests = 'EmptyStringTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'; Find = '.Any(t => access.Span.Contains(t.Span)'; Replace = '.Any(t => false'; Tests = 'EmptyStringTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'; Find = 'is IFieldSymbol { ContainingType.SpecialType: SpecialType.System_String }'; Replace = 'is IFieldSymbol'; Tests = 'EmptyStringTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmptyStringAnalyzer.cs'; Find = '!EmptyStrings.PrefersLiteral(c.Options.AnalyzerConfigOptionsProvider.GetOptions(literal.SyntaxTree))'; Replace = 'true'; Tests = 'EmptyStringTests' }

        # First-run fixes (2026-10-06): BRO1601 conditional directives and doc generation, BRO1603 spaces before a closing
        # tag, BRO1604/BRO1606 text that can't follow the standard words, BRO1134 a comment ending the header's line
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'comments = comments.RemoveAll(c => previous.TrailingTrivia.Contains(c));'; Replace = ''; Tests = 'DeclarationCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'GetDeclarationOpenBrace(openBrace.Parent!) == openBrace && Line('; Replace = 'Line('; Tests = 'DeclarationCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = ' && Line(text, openBrace.SpanStart) != Line(text, previous.SpanStart))'; Replace = ')'; Tests = 'DeclarationCommentTests' }

        # BRO1120 (empty comments between blank lines)
        @{ File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'; Find = '|| !IsBlankLine(text, text.Lines[first.LineNumber - 1]))'; Replace = ')'; Tests = 'CommentTextTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'; Find = 'if (blankEnd > end)'; Replace = 'if (false)'; Tests = 'CommentTextTests' }

        # BRO1132 (embedded comments)
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| Line(text, GetHeaderStart(owner).SpanStart) != Line(text, previous.SpanStart)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| gap.Any(t => t.IsDirective)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| comments.Any(c => Line(text, c.Span.Start) != Line(text, c.Span.End))'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '|| Line(text, openBrace.GetNextToken().SpanStart) == Line(text, openBrace.SpanStart)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '&& !trivia.ToString().StartsWith("////", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'when block.Parent is not null && BlockOwners.Any(block.Parent.IsKind)'; Replace = ''; Tests = 'EmbeddedCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'next.IsKind(SyntaxKind.CloseBraceToken)'; Replace = 'false'; Tests = 'EmbeddedCommentTests' }

        # BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'openBrace.Parent is BlockSyntax or AccessorListSyntax ?'; Replace = 'openBrace.Parent is BlockSyntax ?'; Tests = 'DeclarationCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'while (token.Parent is AttributeListSyntax list && list.Parent == owner)'; Replace = 'while (token.Parent is AttributeListSyntax list && list.Parent == null)'; Tests = 'DeclarationCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = '&& line.Start < next.SpanStart && text.ToString(line.Span).Trim().Length == 0'; Replace = '&& false'; Tests = 'DeclarationCommentTests' }

        # BRO1131 (base calls). Not mutated: Speculation.SymbolAfterReplacing vs a position-based lookup of a DETACHED
        # node (equivalent; the original bug passed the attached access.Name, which binds as a member-access name).
        @{ File = 'src/StyleBro.Analyzers/Readability/BaseCalls.cs'; Find = 'var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;'; Replace = 'var dispatchesVirtually = false;'; Tests = 'BaseCallsTests' }

        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'; Find = 'if (written is NameSyntax argument && StartsWithAlias(argument, model, cancellationToken))'; Replace = 'if (written is NameSyntax argument && false)'; Tests = 'QualifiedUsingsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'; Find = '|| (!top && (type.IsTupleType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T))'; Replace = ''; Tests = 'QualifiedUsingsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'; Find = 'top && type is { SpecialType: not SpecialType.None } ? QualifiedNoKeywords : Qualified'; Replace = 'Qualified'; Tests = 'QualifiedUsingsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = 'if (first == last || item.DescendantTokens()'; Replace = 'if (first == last || false && item.DescendantTokens()'; Tests = 'ParameterLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = 'foreach (var change in Reindent(item, text, indentation))'; Replace = 'foreach (var change in Reindent(item, text, indentation).Take(0))'; Tests = 'ParameterLayoutTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/ParameterLayoutCodeFixProvider.cs'; Find = '!remaining.Any(o => o != l && o.Span.Contains(l.Span))'; Replace = 'true'; Tests = 'ParameterLayoutTests' }

        # Options: stylebro_closing_parenthesis_placement (BRO1110), stylebro_split_list_first_item (BRO1107/BRO1108)
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'if (list.ContainsDirectives)'; Replace = 'if (false)'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'if (!IsPlainGap(last, close))'; Replace = 'if (false)'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'if (Line(text, last.Span.End) != Line(text, close.SpanStart))'; Replace = 'if (false)'; Tests = 'ParenthesisPlacementTests' }

        # Hardening: BRO1110 own_line reindents a ')' on its own line, like the line with '(' after the other fixes
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'return before == indentation || '; Replace = 'return '; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = ' || !string.IsNullOrWhiteSpace(before)'; Replace = ''; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = '&& isOn(DiagnosticIds.ConstructorInitializerLine) && '; Replace = '&& false && '; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = '!ConstructorInitializers.IsSameLine(options) && '; Replace = ''; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'return newText.Substring(lineBreak + 1);'; Replace = 'break;'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'return newText; // reindented where it is'; Replace = 'break;'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'if (startsLine)'; Replace = 'if (false)'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'else if (startsLine && openMoves'; Replace = 'else if (false && openMoves'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = '&& open == current && current.GetNextToken() == close && '; Replace = '&& open == current && '; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = '&& fix.Change.Span.Start == fix.Close.GetPreviousToken().Span.End;'; Replace = ';'; Tests = 'ConstructorInitializerLineTests' }

        # Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'openMoves && GetMisplacedOpen(list, text) is not null ?'; Replace = 'false ?'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'if (Line(text, lastItem.Span.End) == openLine.LineNumber)'; Replace = 'if (false)'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'if (Line(text, lastItem.SpanStart) != openLine.LineNumber || GetOpening(last)'; Replace = 'if (GetOpening(last)'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'SyntaxKind.CloseParenToken when closing.Parent is ArgumentListSyntax or TupleExpressionSyntax'; Replace = 'SyntaxKind.CloseParenToken when false'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'return (false, openingLine == openLine.LineNumber ? null : openLine);'; Replace = 'return (false, openLine);'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = 'GetCloseFix(list, text, ownLine, ownLine && isOn'; Replace = 'GetCloseFix(list, text, false, false && isOn'; Tests = 'FixOrderTests.SameLineJoins' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'; Find = '? (true, openLine) : (false, null);'; Replace = '? (false, openLine) : (false, null);'; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacementAnalyzer.cs'; Find = 'var openMoves = ownLine && '; Replace = 'var openMoves = false && '; Tests = 'ParenthesisPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = ' && !(options is not null && IsSameLine(options))'; Replace = ''; Tests = 'ParameterLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = ' && !(sameLine && previousEnd == open.Span.End))'; Replace = ')'; Tests = 'ParameterLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = 'if (sameLine && items.FirstOrDefault('; Replace = 'if (false && items.FirstOrDefault('; Tests = 'ParameterLayoutTests' }

        # BRO1147 (redundant null-forgiving '!')
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| operand is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression }'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| operand is DefaultExpressionSyntax'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| operand.HasTrailingTrivia'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| node.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not 0 }'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| MultiTargetSuppressor.GetFrameworks(options).Count > 1)'; Replace = '|| MultiTargetSuppressor.GetFrameworks(options).Count > 2)'; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| context.WarningsEnabled() != project.WarningsEnabled()'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '|| context.AnnotationsEnabled() != project.AnnotationsEnabled()'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = 'return IsFlat(model.GetTypeInfo(operand, cancellationToken).Type)'; Replace = 'return true'; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = 'StateWithout(node, model, cancellationToken) == NullableFlowState.NotNull'; Replace = 'info.Nullability.FlowState == NullableFlowState.NotNull'; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = 'named => (named.ContainingType is null || IsFlat(named.ContainingType))'; Replace = 'named => (true)'; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = '&& named.TypeArguments.All(a => a.IsValueType && IsFlat(a))'; Replace = ''; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = 'named.TypeArguments.All(a => a.IsValueType && IsFlat(a))'; Replace = 'named.TypeArguments.Length == 0'; Tests = 'NullForgivingTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'; Find = 'case EqualsValueClauseSyntax clause when clause.Parent is not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax }:'; Replace = 'case EqualsValueClauseSyntax clause when false:'; Tests = 'NullForgivingTests' }

        # BRO1148 (HasValue -> null check)
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '|| node.Span.End - node.Expression.Span.End != HasValue.Length)'; Replace = ')'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = 'if ((negated && node.SpanStart - target.SpanStart != 1)'; Replace = 'if ((false)'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '|| !NullChecks.IsLooseParent(judged)'; Replace = ''; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '|| target.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax'; Replace = '|| target.Parent is CastExpressionSyntax { Parent.Parent: InvocationExpressionSyntax'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '(negated ? LanguageVersion.CSharp7 : LanguageVersion.CSharp9)'; Replace = 'LanguageVersion.CSharp7'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '|| (pattern && ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion'; Replace = '|| (false && ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = 'is not IPropertySymbol { ContainingType.OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })'; Replace = 'is not IPropertySymbol)'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '? NullChecks.IsInExpressionTree(target, model, cancellationToken)'; Replace = '? false'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = ': !IsBuiltIn(model, target, node.Expression, negated, cancellationToken))'; Replace = ': false)'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '&& Severities.IsOn(model.Compilation.Options, node.SyntaxTree, DiagnosticIds.UnnecessaryParentheses, cancellationToken)'; Replace = '&& false'; Tests = 'HasValueTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'; Find = '&& Severities.IsOn(model.Compilation.Options, node.SyntaxTree, DiagnosticIds.UnnecessaryParentheses, cancellationToken)'; Replace = '&& true'; Tests = 'HasValueTests' }

        # Generated code and C# versions (real-world: eShopOnWeb's Razor pages, Mapperly's generated accessors, LibGit2Sharp's net472)
        @{ File = 'src/StyleBro.CodeFixes/Readability/NullCheckCodeFixProvider.cs'; Find = 'NullChecks.GetFix(e, model, options, cancellationToken, version)'; Replace = 'NullChecks.GetFix(e, model, options, cancellationToken)'; Tests = 'NullCheckTests' }

        # Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
        # BRO1105 / BRO1111 same_line
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = '(!allowAdjacentSingleLine || multiLinePrefix))'; Replace = '(multiLinePrefix))'; Tests = 'CombinedFieldsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = '(!allowAdjacentSingleLine || multiLinePrefix))'; Replace = '(!allowAdjacentSingleLine))'; Tests = 'CombinedFieldsTests' }

        # Fix order: BRO1114's blank line above a documented field's copies (BRO1513)
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = ': (docText.Length > 0'; Replace = ': (false'; Tests = 'CombinedFieldsTests' }

        # Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = 'joined is null || !joinsEmptyLists() ? Plain'; Replace = 'joined is null ? Plain'; Tests = 'ParameterLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'; Find = 'Plain(position) - joined.Where(j => j.At <= position).Sum(j => j.Breaks)'; Replace = 'Plain(position)'; Tests = 'ParameterLayoutTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/CombinedFieldsCodeFixProvider.cs'; Find = 'CombinedFields.GetChange(field, text, allowAdjacent)'; Replace = 'CombinedFields.GetChange(field, text)'; Tests = 'CombinedFieldsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'out var value) && value.Trim() == "same_line";'; Replace = 'out var value);'; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = '|| Line(text, close.Span.End) == Line(text, keyword.SpanStart)'; Replace = ''; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = '|| Line(text, keyword.SpanStart) != Line(text, initializer.Span.End)'; Replace = ''; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = '|| !IsPlain(close.TrailingTrivia, '; Replace = '|| false && !IsPlain(close.TrailingTrivia, '; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'if (isOn(DiagnosticIds.CloseParenthesisOnLastItemLine) && '; Replace = 'if ('; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'if (!IsPlain(last.TrailingTrivia, close.LeadingTrivia))'; Replace = 'if (false)'; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = '(initializer.Span.End - keyword.SpanStart) > maxLength'; Replace = '(initializer.Span.End - keyword.SpanStart) >= maxLength'; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'return maxLength != int.MaxValue'; Replace = 'return false'; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'start, closeText.Length > 0, text'; Replace = 'start, false, text'; Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAfterBro1110' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'out var value) && value.Trim() == "same_line";'; Replace = 'out var value);'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = '&& isOn(DiagnosticIds.CloseParenthesisOnLastItemLine)'; Replace = ''; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'if (!IsPlainGap(previous.GetPreviousToken(), previous))'; Replace = 'if (false)'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'if (Line(text, clause.SpanStart) != Line(text, clause.Span.End) || '; Replace = 'if ('; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = '|| !IsPlainGap(before, clause.WhereKeyword))'; Replace = ')'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'length += clause.WhereKeyword.SpanStart - before.Span.End + clause.Span.Length;'; Replace = 'length += 0;'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'length += 1 + clause.Span.Length;'; Replace = 'length += 0;'; Tests = 'ConstraintPlacementTests' }

        # A comment below code (BRO1504, BRO1506, BRO1001; eShop)
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = '+ length > maxLength ? [] : joins;'; Replace = '+ length >= maxLength ? [] : joins;'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'return joins.Count > 0 && maxLength != int.MaxValue'; Replace = 'return joins.Count > 0 && false'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = '&& isOn(finding.Id))'; Replace = ')'; Tests = 'ConstraintPlacementTests.SameLine_WithoutBro1404AndBro1007' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = 'finding.Modifier + " "'; Replace = 'string.Empty'; Tests = 'ConstraintPlacementTests.SameLine_NotReported_WhenAnAddedModifier' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = 'if (isOn(DiagnosticIds.OpenParenthesisOnNameLine) && ParenthesisPlacement'; Replace = 'if (ParenthesisPlacement'; Tests = 'ConstructorInitializerLineTests.SameLine_NotReported_WhenTheRulesThatShortenTheLineAreOff' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = 'changes.Add(ParenthesisPlacement.GetOpenChange(open, text));'; Replace = ''; Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAsTheOtherFixesLeaveTheLine' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = 'if (isOn(DiagnosticIds.ParametersOnSameOrSeparateLines)'; Replace = 'if (true'; Tests = 'ConstructorInitializerLineTests.SameLine_NotReported_WhenTheRulesThatShortenTheLineAreOff' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = 'return isOn(DiagnosticIds.CloseParenthesisOnLastItemLine)'; Replace = 'return true'; Tests = 'ConstructorInitializerLineTests.SameLine_NotReported_WhenTheRulesThatShortenTheLineAreOff' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = 'if (closeMoves && close?.NewText'; Replace = 'if (false && close?.NewText'; Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAfterBro1110' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = '|| applied[applied.Count - 1].Span.End <= change.Span.Start)'; Replace = '|| true)'; Tests = 'ConstructorInitializerLineTests.SameLine_MeasuredAsTheOtherFixesLeaveTheLine' }
        @{ File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'; Find = '.Where(c => c.Span.End <= position)'; Replace = '.Where(c => false)'; Tests = 'ConstraintPlacementTests.SameLine_NotReported_WhenAnAddedModifier' }

        # BRO1150: List/array Exists and Find where Sonar S6605/S6602 are on
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = '|| !SonarRules.IsOn(model, options, name == "Any" ? "S6605" : "S6602", cancellationToken))'; Replace = ')'; Tests = 'WhereBeforeTerminalTests.SonarCollectionRules' }
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = '|| !(isArray ? symbol.ContainingType.SpecialType == SpecialType.System_Array : IsList(symbol.ContainingType)))'; Replace = ')'; Tests = 'WhereBeforeTerminalTests.CollectionMethodsThatWouldNotBind' }
        @{ File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'; Find = 'return Trivia.IsBlank(terminal, TextSpan.FromBounds(receiver.Span.End, access.Name.SpanStart))'; Replace = 'return true'; Tests = 'WhereBeforeTerminalTests.CollectionMethodsThatWouldNotBind' }
    )
}
