# Mutations for Documentation (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
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

        # BRO1603 (periods): closing punctuation, entities, excluded tags; BRO1606: a summary starting with <para>
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '.TrimEnd(ClosingPunctuation);'; Replace = ';'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 't.Kind() is SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlEntityLiteralToken'; Replace = 't.IsKind(SyntaxKind.XmlTextLiteralToken)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '&& !excluded.Contains(name) && '; Replace = '&& '; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = ' || excluded.Contains(name) || IsQuotedSentence'; Replace = ' || IsQuotedSentence'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'GetTextStart(paragraph ?? summary'; Replace = 'GetTextStart(summary'; Tests = 'DocumentationTests' }

        # BRO1106 literal style
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = '? [verb, "Gets"] : [verb]'; Replace = '? [verb] : [verb]'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = ' || old.StartsWith("Gets or initializes", StringComparison.Ordinal)'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ParameterDocumentation.cs'; Find = 'TypeDeclarationSyntax type => type.ParameterList,'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'return paragraph is null ? GetBlankFinding(member, summary, text, standard) : null;'; Replace = 'return null;'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'if (!Regex.IsMatch(prefix, '; Replace = 'if (false && Regex.IsMatch(prefix, '; Tests = 'DocumentationTests' }

        # Following StyleCop master (owner's decision 2026-10-04): single-line properties together, no inheritdoc on explicit implementations
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
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = "end + 1 < text.Length && text[end] == '<' && text[end + 1] == '/' ? end : position"; Replace = 'position'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = 'if (old.Length == 0 && (StartsWithAnyWord(rest, Conditions) || StartsWithAnyWord(rest, Verbs)))'; Replace = 'if (false)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = ' || StartsWithAnyWord(rest, Verbs)))'; Replace = '))'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = 'if (old.Length == 0 && !char.IsLower(rest[0]) && !StartsWithAnyWord(rest, Articles))'; Replace = 'if (false)'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = '&& !StartsWithAnyWord(rest, Articles))'; Replace = ')'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'; Find = '!char.IsLower(rest[0]) && '; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = '(char.IsLower(remaining[0]) || '; Replace = '('; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = ' || PropertySummaries.StartsWithAnyWord(remaining, PropertySummaries.Conditions)))'; Replace = '))'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = "&& (StartsWithWords(remaining, standard.Substring(0, standard.IndexOf(' ')))"; Replace = '&& (false'; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = 'if (replaceLength == 0'; Replace = 'if (true'; Tests = 'DocumentationTests' }

        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = '|| IsQuotedSentence(child)'; Replace = ''; Tests = 'DocumentationTests' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 'text[text.Length - 1] is ''"'' or ''\'''''; Replace = 'true'; Tests = 'DocumentationTests' }

        # Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
        # BRO1105 / BRO1111 same_line
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'; Find = 'style.Trim() == "spaced"'; Replace = 'style.Trim() != "spaced"'; Tests = 'DocumentationTests' }

        # BRO1603: elements StyleCop doesn't know are passed over
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 'if (InlineElements.Contains(name))'; Replace = 'if (true)'; Tests = 'DocumentationTests.UnknownElements_ArePassedOver' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = 'if (InlineElements.Contains(emptyName))'; Replace = 'if (true)'; Tests = 'DocumentationTests.UnknownElements_ArePassedOver' }
        @{ File = 'src/StyleBro.Analyzers/Documentation/DocumentationPeriods.cs'; Find = "&& text[end] == '<' && text[end + 1] == '/'"; Replace = "&& text[end] == '<'"; Tests = 'DocumentationTests.UnknownElements_ArePassedOver' }

        # A documented property is set apart (BRO1505), BRO1601 adds the blank line below (Ocelot)
        @{ File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'; Find = 'if (separate && member is PropertyDeclarationSyntax property'; Replace = 'if (false && member is PropertyDeclarationSyntax property'; Tests = 'DocumentationTests.InheritDoc_OnAdjacentProperties_SeparatesThem' }
        @{ File = 'src/StyleBro.CodeFixes/Documentation/DocumentationCodeFixProvider.cs'; Find = '&& !(blankLineBefore && property.Parent is TypeDeclarationSyntax type && documented.Contains('; Replace = '&& !(false && property.Parent is TypeDeclarationSyntax type && documented.Contains('; Tests = 'DocumentationTests.InheritDoc_OnAdjacentProperties_SeparatesThem' }

        # BRO1606: constructor sentences of their own (Instantiates, Constructs, Creates a new)
        @{ File = 'src/StyleBro.Analyzers/Documentation/ConstructorSummaries.cs'; Find = '|| (member is ConstructorDeclarationSyntax && ConstructorVerbs.Any(v => StartsWithWords(remaining, v)))))'; Replace = '))'; Tests = 'DocumentationTests.SummariesThatCantFollowTheStandardWords_AreNotReported' }
    )
}
