# Mutations for scripts/mutation/Invoke-Mutations.ps1: each breaks one guard on purpose (Find must occur exactly once in
# File; Replace takes its place), and at least one test in Tests (a test class filter) must then fail. A mutation that
# survives is a guard no test covers. Add one for every new guard.
@{
    Mutations = @(
        # Files from NuGet packages (PackageFileSuppressor)
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = '? roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))'; Replace = '? true'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = 'file.IndexOf("/contentFiles/", StringComparison.OrdinalIgnoreCase) >= 0'; Replace = 'false'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = 'return roots.Count > 0'; Replace = 'return false'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = 'if (isPackageFile)'; Replace = 'if (true)'; Tests = 'PackageFileSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/PackageFileSuppressor.cs'; Find = ".Replace('\\', '/').TrimEnd('/'))"; Replace = ".Replace('\\', '/'))"; Tests = 'PackageFileSuppressorTests' }

        # Multi-target guard (MultiTargetSuppressor)
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'if (frameworks.Count < 2)'; Replace = 'if (frameworks.Count < 1)'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = '!frameworks.All(f => Has(f, Minimums[diagnostic.Id]))'; Replace = '!frameworks.Any(f => Has(f, Minimums[diagnostic.Id]))'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'Descriptors.TryGetValue(diagnostic.Id, out var descriptor) && '; Replace = 'Descriptors.TryGetValue("CA1510", out var descriptor) && '; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'var name = framework.ToLowerInvariant();'; Replace = 'var name = framework;'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'if (dash >= 0)'; Replace = 'if (false)'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'out var core) && core >= minimum.Core'; Replace = 'out var core)'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = 'minimum.Standard is { } standard && Version.TryParse(name.Substring(11), out var version) && version >= standard'; Replace = 'minimum.Standard is { } standard'; Tests = 'MultiTargetSuppressorTests' }
        @{ File = 'src/StyleBro.Analyzers/Modernize/MultiTargetSuppressor.cs'; Find = '&& net.Major >= 5 && net >= minimum.Core'; Replace = '&& net >= minimum.Core'; Tests = 'MultiTargetSuppressorTests' }
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
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken)'; Replace = 'false'; Tests = 'FixOrderTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'; Find = 'if (BlankLineRuns.JudgesGapAfter(close, next, text, isOn, gapIsReplaced: false))'; Replace = 'if (false)'; Tests = 'AutoAccessorLinesTests' }
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
        # BRO1408 (redundant base type)
        @{ File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'; Find = 'EnumUnderlyingType.SpecialType: SpecialType.System_Int32'; Replace = 'EnumUnderlyingType: not null'; Tests = 'RedundantBaseTypeTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'; Find = '.Type?.SpecialType == SpecialType.System_Object'; Replace = '.Type is not null'; Tests = 'RedundantBaseTypeTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/RedundantBaseTypes.cs'; Find = 'return Trivia.IsBlank(declaration, span) ?'; Replace = 'return true ?'; Tests = 'RedundantBaseTypeTests' }
        # BRO1617-BRO1619 (documentation style) and BRO1504's exempt prefixes
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'if (cref.ContainsDiagnostics)'; Replace = 'if (false)'; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'token.Text != entity || '; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'token.Parent is not (TypeArgumentListSyntax or TypeParameterListSyntax)'; Replace = 'false'; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'return changes.Count == 0 ? null :'; Replace = 'return false ? null :'; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'if (element.StartTag.Name.LocalName.ValueText != "c"'; Replace = 'if (false'; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = '|| element.StartTag.Attributes.Count > 0'; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = '|| element.EndTag.Name.LocalName.ValueText != "c"'; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = '{ TextTokens.Count: 1 }'; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = '&& !word.StartsWith("__", StringComparison.Ordinal)'; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = '|| ContextualKeywords.Contains(word)'; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'DescendantNodes(n => !IsCode(n))'; Replace = 'DescendantNodes()'; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'if (plain.TextTokens.Any('; Replace = 'if (false && plain.TextTokens.Any('; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = 'if (rank < 0)'; Replace = 'if (false)'; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyle.cs'; Find = ' || !elements.All(e => ParameterDocumentation.IsOnOwnLines(e.Node, text))'; Replace = ''; Tests = 'DocumentationStyleTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = 'if (exemptPrefixes.Any(p => commentText.StartsWith(p, StringComparison.Ordinal)))'; Replace = 'if (false)'; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = 'commentText.StartsWith(p, StringComparison.Ordinal)'; Replace = 'commentText.StartsWith(p, StringComparison.OrdinalIgnoreCase)'; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '.Where(p => p.Length > 0)'; Replace = ''; Tests = 'BlankLineAfterTests' }

        # BRO1616 (summary layout)
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'if (end.Name.LocalName.ValueText != "summary")'; Replace = 'if (false)'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'if (prefix.Trim() != "///" || '; Replace = 'if ('; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = ' || text.ToString(TextSpan.FromBounds(end.Span.End, endLine.End)).Trim().Length > 0)'; Replace = ')'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'if (segments.Count == 0)'; Replace = 'if (false)'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'return singleLine ? null :'; Replace = 'return false ? null :'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = '&& segments.Count == 1'; Replace = ''; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = '&& !summary.Content.SelectMany(c => c.DescendantNodesAndSelf()).Any(IsBlockElement)'; Replace = ''; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = '&& (start.Span.End - startLine.Start) + first.Length + end.Span.Length <= maxLength'; Replace = ''; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'return textAfterStart || textBeforeEnd ?'; Replace = 'return true ?'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'if (afterStart)'; Replace = 'if (true)'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'if (beforeEnd)'; Replace = 'if (true)'; Tests = 'SummaryLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'; Find = 'if (line.Start >= contentStart)'; Replace = 'if (false)'; Tests = 'SummaryLayoutTests' }

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
        # BRO1001 stylebro_keep_overloads_together (Sonar S4136)
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (options.KeepOverloadsTogether)'; Replace = 'if (true)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = ' || keys[i].CompareTo(keys[anchor]) < 0))'; Replace = '))'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '? (segments[i], method.'; Replace = '? (0, method.'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (anchors is not null && anchors[a] != anchors[b])'; Replace = 'if (false)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '.ThenBy(i => anchors[i]).'; Replace = '.'; Tests = 'MemberOrderingTests' }
        # BRO1524 (split conditional expressions)
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (conditional.ContainsDiagnostics'; Replace = 'if (false'; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = '|| conditional.WhenFalse is ConditionalExpressionSyntax'; Replace = ''; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = '|| (conditional.Parent is ConditionalExpressionSyntax parent && parent.WhenFalse == conditional)'; Replace = ''; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (gaps.SelectMany(g => g).Any('; Replace = 'if (false && gaps.SelectMany(g => g).Any('; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (before || after || Line(text, part.SpanStart) != Line(text, part.Span.End))'; Replace = 'if (before || after)'; Tests = 'ConditionalLayoutTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'; Find = 'if (before || after || '; Replace = 'if (after || '; Tests = 'ConditionalLayoutTests' }
        # BRO1603 (periods): closing punctuation, entities, excluded tags; BRO1606: a summary starting with <para>
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '.TrimEnd(ClosingPunctuation);'; Replace = ';'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 't.Kind() is SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlEntityLiteralToken'; Replace = 't.IsKind(SyntaxKind.XmlTextLiteralToken)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '&& !excluded.Contains(name) && '; Replace = '&& '; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = ' || excluded.Contains(name) || IsQuotedSentence'; Replace = ' || IsQuotedSentence'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'GetTextStart(paragraph ?? summary'; Replace = 'GetTextStart(summary'; Tests = 'DocumentationTests' }

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
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = '? [verb, "Gets"] : [verb]'; Replace = '? [verb] : [verb]'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = ' || old.StartsWith("Gets or initializes", StringComparison.Ordinal)'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ParameterDocumentation.cs'; Find = 'TypeDeclarationSyntax type => type.ParameterList,'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'return paragraph is null ? GetBlankFinding(member, summary, text, standard) : null;'; Replace = 'return null;'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'if (!Regex.IsMatch(prefix, '; Replace = 'if (false && Regex.IsMatch(prefix, '; Tests = 'DocumentationTests' }

        # BRO1510: an attribute line doesn't make an accessor multi-line
        @{ File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'; Find = '(accessor.Modifiers.Count > 0 ? accessor.Modifiers[0] : accessor.Keyword).SpanStart'; Replace = 'accessor.SpanStart'; Tests = 'AccessorLayoutTests' }

        # BRO1505: a field below a field that spans several lines; BRO1001's sort and BRO1114's split add that blank line
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '=> SpansSeveralLines(field, text),'; Replace = '=> false,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'member.AttributeLists.Count > 0 ? member.AttributeLists.Last().FullSpan.End : member.SpanStart'; Replace = 'member.SpanStart'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'return Layout.ElementSeparation.NeedsBlankLine(previous, current, previous.SyntaxTree.GetText(), autoAccessorLines, allowAdjacentSingleLine);'; Replace = 'return !(previous is FieldDeclarationSyntax && current is FieldDeclarationSyntax);'; Tests = 'FixOrderTests' }
        # Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '(PropertyDeclarationSyntax property, PropertyDeclarationSyntax next) => IsMultiLineProperty(property, text, autoAccessorLines) || IsMultiLineProperty(next, text, autoAccessorLines),'; Replace = ''; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'IsMultiLineProperty(property, text, autoAccessorLines) || IsMultiLineProperty(next, text, autoAccessorLines),'; Replace = 'IsMultiLineProperty(property, text, autoAccessorLines),'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = '!IsExplicitImplementation(s) && '; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = 'if (blankLineBefore && indentation.Trim().Length == 0'; Replace = 'if (false && indentation.Trim().Length == 0'; Tests = 'DocumentationTests' }

        # First-run fixes (2026-10-06): BRO1601 conditional directives and doc generation, BRO1603 spaces before a closing
        # tag, BRO1604/BRO1606 text that can't follow the standard words, BRO1134 a comment ending the header's line
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationAnalyzer.cs'; Find = '|| DocumentationComments.HasConditionalDirective((MemberDeclarationSyntax)context.Node)'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = 'var end = member.AttributeLists.Count > 0 ? member.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart : member.SpanStart;'; Replace = 'var end = member.SpanStart;'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationAnalyzer.cs'; Find = '|| !DocumentationComments.GeneratesDocumentation(context.Node.SyntaxTree, options)'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = '? string.Equals(value.Trim(), "true", System.StringComparison.OrdinalIgnoreCase)'; Replace = '? true'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = ': tree.Options.DocumentationMode >= DocumentationMode.Parse;'; Replace = ': true;'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = '&& value.Trim().Length > 0'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = "end < text.Length && text[end] == '<' ? end : position"; Replace = 'position'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = 'if (old.Length == 0 && (StartsWithAnyWord(rest, Conditions) || StartsWithAnyWord(rest, Verbs)))'; Replace = 'if (false)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = ' || StartsWithAnyWord(rest, Verbs)))'; Replace = '))'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = 'if (old.Length == 0 && !char.IsLower(rest[0]) && !StartsWithAnyWord(rest, Articles))'; Replace = 'if (false)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = '&& !StartsWithAnyWord(rest, Articles))'; Replace = ')'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = '!char.IsLower(rest[0]) && '; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = '(char.IsLower(remaining[0]) || '; Replace = '('; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = ' || PropertySummaries.StartsWithAnyWord(remaining, PropertySummaries.Conditions)))'; Replace = '))'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = "if (replaceLength == 0 && StartsWithWords(remaining, standard.Substring(0, standard.IndexOf(' '))))"; Replace = 'if (false)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'if (replaceLength == 0 && StartsWithWords(remaining,'; Replace = 'if (StartsWithWords(remaining,'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'comments = comments.RemoveAll(c => previous.TrailingTrivia.Contains(c));'; Replace = ''; Tests = 'DeclarationCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = 'GetDeclarationOpenBrace(openBrace.Parent!) == openBrace && Line('; Replace = 'Line('; Tests = 'DeclarationCommentTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'; Find = ' && Line(text, openBrace.SpanStart) != Line(text, previous.SpanStart))'; Replace = ')'; Tests = 'DeclarationCommentTests' }

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
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = '.Where(d => !d.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))'; Replace = ''; Tests = 'MigrationTests.EveryStyleBroRule_NamesTheStyleCopRulesItReplaces' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'; Find = ' || node.IsPartOfStructuredTrivia()'; Replace = ''; Tests = 'PrecedenceTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/PrecedenceAnalyzer.cs'; Find = 'if (!Precedence.IsWanted(id, options))'; Replace = 'if (false)'; Tests = 'PrecedenceTests' }

        # BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 2)'; Replace = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 0)'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'trivia[start - 2].IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'stop.IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = 'brace.RawKind != 0 && isOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'false'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = ' || owner is DoStatementSyntax or BlockSyntax { Parent: DoStatementSyntax }'; Replace = ''; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '(gapIsReplaced && t.IsKind(SyntaxKind.EndOfLineTrivia))'; Replace = 'false'; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '(runs ??= []).Add(found);'; Replace = '{ (runs ??= []).Add(found); return runs; }'; Tests = 'BlankLineRunsTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'; Find = '&& TrailingBlankLines.GetBlankLinesAfterComment(trivia[start - 2], text).Count > 0)'; Replace = ')'; Tests = 'BlankLineRunsTests' }

        # BRO1120 (empty comments between blank lines)
        @{ File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'; Find = '|| !IsBlankLine(text, text.Lines[first.LineNumber - 1]))'; Replace = ')'; Tests = 'CommentTextTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'; Find = 'if (blankEnd > end)'; Replace = 'if (false)'; Tests = 'CommentTextTests' }

        # BRO1001: comments that introduce a group (GroupComments)
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '|| GroupComments.MovesGroupComment(members, keys, order)'; Replace = ''; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = 'if (keys[i + 1].Kind == keys[i].Kind && '; Replace = 'if ('; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = '&& Moves(position, i) && HasComment'; Replace = '&& HasComment'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = 'if (trivia.IsDirective)'; Replace = 'if (false)'; Tests = 'MemberOrderingTests' }

        # BRO1309: snake_case names; a property kept by a string keeps its type's other properties
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '|| (symbol is IPropertySymbol { ContainingType: { } owner } && HasKeptProperty(owner, strings!, keptTypes))'; Replace = ''; Tests = 'PascalCaseNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '.Any(p => PascalCaseNamingAnalyzer.GetNewName(p.Name) is not null && IsInStrings(p, strings))'; Replace = '.Any(p => IsInStrings(p, strings))'; Tests = 'PascalCaseNamingTests' }
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
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'declaration.ContainsDirectives ? TreeWalk.Trivia('; Replace = 'false ? TreeWalk.Trivia('; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'facts.Texts.Add(list[j].ToString());'; Replace = ''; Tests = 'FieldNamingTests' }
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
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '!(Readability.EmbeddedComments.GetMovingRule(comment, text) is { } rule && isOn(rule))'; Replace = 'true'; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(previous.IsKind(SyntaxKind.OpenBracketToken) && previous.Parent.IsKind(SyntaxKind.CollectionExpression))'; Replace = ''; Tests = 'BlankLineAfterTests' }

        # BRO1131 (base calls). Not mutated: Speculation.SymbolAfterReplacing vs a position-based lookup of a DETACHED
        # node (equivalent; the original bug passed the attached access.Name, which binds as a member-access name).
        @{ File = 'src/StyleBro.Analyzers/Readability/BaseCalls.cs'; Find = 'var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;'; Replace = 'var dispatchesVirtually = false;'; Tests = 'BaseCallsTests' }

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
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'; Find = 'argument.Parent?.Parent is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } }'; Replace = 'false'; Tests = 'ParameterMatchesBaseTests' }
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
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'if (symbols.Any(s => CamelCaseNamingAnalyzer.GetBaseMembers(s).Any(b => !renamed.Contains(b.OriginalDefinition.ToDisplayString())))
                ||'; Replace = 'if ('; Tests = 'AsyncSuffixTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '                    safe = false;
                    break;'; Replace = '                    continue;'; Tests = 'LinkedFileFixAllTests' }

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

        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'; Find = 'if (written is NameSyntax argument && StartsWithAlias(argument, model, cancellationToken))'; Replace = 'if (written is NameSyntax argument && false)'; Tests = 'QualifiedUsingsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'; Find = '|| (!top && (type.IsTupleType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T))'; Replace = ''; Tests = 'QualifiedUsingsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'; Find = 'top && type is { SpecialType: not SpecialType.None } ? QualifiedNoKeywords : Qualified'; Replace = 'Qualified'; Tests = 'QualifiedUsingsTests' }
        @{ File = 'src/StyleBro.Analyzers/Maintainability/AccessModifiers.cs'; Find = ' || symbol is INamedTypeSymbol { IsFileLocal: true })'; Replace = ')'; Tests = 'AccessModifiersTests' }
        @{ File = 'src/StyleBro.Analyzers/Spacing/CommentSpacing.cs'; Find = '|| comment.StartsWith("//-:", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'CommentSpacingTests' }
        @{ File = 'src/StyleBro.Analyzers/Spacing/CommentSpacing.cs'; Find = '|| comment.StartsWith("//+:", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'CommentSpacingTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !previous.IsKind(SyntaxKind.EqualsGreaterThanToken)'; Replace = ''; Tests = 'BlankLineAfterTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'; Find = '&& !(token.Parent is InitializerExpressionSyntax { Parent: InitializerExpressionSyntax })'; Replace = ''; Tests = 'BlankLineBeforeTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'; Find = '&& !HungarianNames.IsExternParameter(context.Node)'; Replace = ''; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/HungarianNames.cs'; Find = '(modifiers.Any(SyntaxKind.ExternKeyword)'; Replace = '(false'; Tests = 'HungarianNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '|| IsQuotedSentence(child)'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 'text[text.Length - 1] is ''"'' or ''\'''''; Replace = 'true'; Tests = 'DocumentationTests' }
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
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = '= await ApplyLayoutAsync(document, root, new HashSet<SyntaxNode>(partialAccess.Keys), cancellationToken).ConfigureAwait(false);'; Replace = '= (document, root, partialAccess.Keys.ToDictionary(k => k, k => k));'; Tests = 'FixOrderTests.Regions_RemovedAroundMembersMissingBlankLines' }
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
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'else if (withNameof && token'; Replace = 'else if (false && token'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = 'if (!Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.MemberOrdering, cancellationToken))'; Replace = 'if (true)'; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = '.Where(s => changes.Any(c => s.Span.Contains(c.Span)))'; Replace = ''; Tests = 'InternalTypeMethodTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'; Find = 'return derived.Any(d => d.Locations.Any(l => l.IsInSource && project.GetDocument(l.SourceTree) is null));'; Replace = 'return false;'; Tests = 'InternalTypeMethodTests' }
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
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = ' || NamespaceNames.HasGeneratedPart(type))'; Replace = ')'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'; Find = '|| ((symbol as INamedTypeSymbol ?? symbol.ContainingType) is { } owner && NamespaceNames.HasGeneratedPart(owner))'; Replace = ''; Tests = 'PascalCaseNamingTests' }
        @{ File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'; Find = '&& (symbol.DeclaringType is not { } owner || !NamespaceNames.HasGeneratedPart(owner))'; Replace = ''; Tests = 'PrefixNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'if (document is null or SourceGeneratedDocument'; Replace = 'if (document is null'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = '|| NamespaceNames.IsGenerated(tree))'; Replace = '|| false)'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'; Find = 'documents.AddRange(await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false));'; Replace = ''; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Readability/NullCheckCodeFixProvider.cs'; Find = 'NullChecks.GetFix(e, model, options, cancellationToken, version)'; Replace = 'NullChecks.GetFix(e, model, options, cancellationToken)'; Tests = 'NullCheckTests' }
        @{ File = 'src/StyleBro.CodeFixes/Maintainability/AccessModifiersCodeFixProvider.cs'; Find = 'preference, version)'; Replace = 'preference)'; Tests = 'AccessModifiersTests' }
        @{ File = 'src/StyleBro.CodeFixes/LinkedFileFixAllProvider.cs'; Find = '.Min();'; Replace = '.Max();'; Tests = 'NullCheckTests' }
        # stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
        @{ File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'; Find = 'if (options.TryGetValue(StaticStyleKey, out var value))'; Replace = 'if (false && options.TryGetValue(StaticStyleKey, out var value))'; Tests = 'FieldNamingTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '&& !IsNestedRepository(child)'; Replace = ''; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '(defaultSeverity > Severity.None || severity == Severity.None)'; Replace = 'true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = 'if (setup.IsOn("SA1633") && textOff.Count == 0)'; Replace = 'if (setup.IsOn("SA1633"))'; Tests = 'MigrationTests' }
        # stylebro-migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
        # folder rulesets, init's field style
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = ' || CompanyName(setup) is null))'; Replace = '))'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = '!config.Bulk.Contains(p.Key) || !specific.Contains(p.Key)'; Replace = 'true'; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/Migration.cs'; Find = ' || !order.Contains("accessibility")'; Replace = ''; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/StyleCopSetup.cs'; Find = 'rulesets.Add([]);'; Replace = ''; Tests = 'MigrationTests' }
        @{ File = 'src/StyleBro.Migrate/InitCommand.cs'; Find = 'underscore >= UnderscoreShare * (underscore + plain)'; Replace = 'underscore > plain'; Tests = 'MigrationTests' }
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
        # Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
        # BRO1105 / BRO1111 same_line
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = 'style.Trim() == "spaced"'; Replace = 'style.Trim() != "spaced"'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'out var value) && value.Trim() == "true";'; Replace = 'out var value);'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'if (allowAdjacentSingleLine && IsCompact(previous, text, autoAccessorLines) && IsCompact(current, text, autoAccessorLines))'; Replace = 'if (allowAdjacentSingleLine)'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'MethodDeclarationSyntax { ConstraintClauses.Count: > 0 } => false,'; Replace = ''; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '&& method.Body is null && !SpansSeveralLines(method, text),'; Replace = '&& !SpansSeveralLines(method, text),'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = '&& method.Body is null && !SpansSeveralLines(method, text),'; Replace = '&& method.Body is null,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'PropertyDeclarationSyntax property => !HasBlockAccessor(property) && '; Replace = 'PropertyDeclarationSyntax property => '; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'or EventDeclarationSyntax && !HasBlockAccessor(other) && '; Replace = 'or EventDeclarationSyntax && '; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'; Find = 'EventFieldDeclarationSyntax eventField => !SpansSeveralLines(eventField, text),'; Replace = 'EventFieldDeclarationSyntax eventField => true,'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/ElementSeparationAnalyzer.cs'; Find = 'var allowAdjacent = ElementSeparation.AllowsAdjacentSingleLineMembers(options);'; Replace = 'var allowAdjacent = false;'; Tests = 'ElementSeparationTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'StyleBro.Analyzers.Layout.ElementSeparation.AllowsAdjacentSingleLineMembers(document'; Replace = 'false && StyleBro.Analyzers.Layout.ElementSeparation.AllowsAdjacentSingleLineMembers(document'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'; Find = 'allowAdjacentSingleLine: ElementSeparation.AllowsAdjacentSingleLineMembers(options)'; Replace = 'allowAdjacentSingleLine: false'; Tests = 'SingleLineBlocksTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = '(!allowAdjacentSingleLine || multiLinePrefix))'; Replace = '(multiLinePrefix))'; Tests = 'CombinedFieldsTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = '(!allowAdjacentSingleLine || multiLinePrefix))'; Replace = '(!allowAdjacentSingleLine))'; Tests = 'CombinedFieldsTests' }
        # Fix order: BRO1114's blank line above a documented field's copies (BRO1513)
        @{ File = 'src/StyleBro.Analyzers/Readability/CombinedFields.cs'; Find = ': (docText.Length > 0'; Replace = ': (false'; Tests = 'CombinedFieldsTests' }
        # Fix order: BRO1001 makes BRO1509's expansion and BRO1505's blank lines before sorting; a one-line type stays on one line
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'if ((own.Count > 0 || IsOn(DiagnosticIds.SingleLineElement))'; Replace = 'if ((own.Count > 0)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'if ((own.Count > 0 || IsOn(DiagnosticIds.SingleLineElement))'; Replace = 'if ((IsOn(DiagnosticIds.SingleLineElement))'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'var violations = IsOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'var violations = false'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'var violations = IsOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'var violations = true'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = '.Sum(c => c.NewText!.Length - c.Span.Length);'; Replace = '.Sum(c => 0);'; Tests = 'FixOrderTests.MemberOrder_BlankLinesInOtherSlots' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (slot > 0 && !oneLine && '; Replace = 'if (slot > 0 && '; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (slot < count - 1 && !oneLine && '; Replace = 'if (slot < count - 1 && '; Tests = 'MemberOrderingTests' }
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
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'return length > Indentation.GetMaxLineLength(options) ?'; Replace = 'return length >= Indentation.GetMaxLineLength(options) ?'; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstructorInitializers.cs'; Find = 'return length > Indentation.GetMaxLineLength(options) ?'; Replace = 'return false ?'; Tests = 'ConstructorInitializerLineTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'out var value) && value.Trim() == "same_line";'; Replace = 'out var value);'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = '&& isOn(DiagnosticIds.CloseParenthesisOnLastItemLine)'; Replace = ''; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'if (!IsPlainGap(previous.GetPreviousToken(), previous))'; Replace = 'if (false)'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'if (Line(text, clause.SpanStart) != Line(text, clause.Span.End) || '; Replace = 'if ('; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = '|| !IsPlainGap(before, clause.WhereKeyword))'; Replace = ')'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'length += clause.WhereKeyword.SpanStart - before.Span.End + clause.Span.Length;'; Replace = 'length += 0;'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'length += 1 + clause.Span.Length;'; Replace = 'length += 0;'; Tests = 'ConstraintPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'; Find = 'return length > Indentation.GetMaxLineLength(options) ? [] : joins;'; Replace = 'return length >= Indentation.GetMaxLineLength(options) ? [] : joins;'; Tests = 'ConstraintPlacementTests' }
    )
}
