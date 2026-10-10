# Mutations for Ordering (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # BRO1008 (using placement)
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '|| root.Members.Count != 1 ||'; Replace = '|| root.Members.Count < 1 ||'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '.LineNumber == lastLine)'; Replace = '.LineNumber == -1)'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'if (startLine == 0 && mode == UsingPlacementMode.Inside)'; Replace = 'if (false)'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = ' && !aboveIsOpenBrace)'; Replace = ')'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '+ (IsBlank(next) || nextIsClose ? string.Empty : lineBreak);'; Replace = ';'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '|| target.Count > 0 ||'; Replace = '||'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '|| moved.Any(u => u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))'; Replace = ''; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'if (lines[firstLine].Start + indentation.Length != first.SpanStart'; Replace = 'if (false'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'if (!global.GetMembers(first).Any() || namespacesOfN.Any(n => n.GetMembers(first).Any()))'; Replace = 'if (false)'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'fromContainers.Count > 0 ? !fromContainers.SetEquals(fromU) :'; Replace = 'fromContainers.Count > 0 ? false :'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = ': !g.Find(name).IsSubsetOf(fromU))'; Replace = ': false)'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '&& !g.Extensions(name).Concat('; Replace = '&& false && !g.Extensions(name).Concat('; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '&& !IsDirective(lines[startLine - 1])'; Replace = ''; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '&& !IsDirective(lines[insertLine - 1])'; Replace = ''; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '&& !usings.IntersectsWith(t.Span))'; Replace = ')'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '(t.IsKind(SyntaxKind.RegionDirectiveTrivia) || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)) && !usings'; Replace = 'true && !usings'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '&& Depth(usings.Start) == 0'; Replace = ''; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = '&& Depth(insertAt) == 0;'; Replace = ';'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'return RegionsOnly(root, checkedEnd, moved, next.Start) ? changes : null;'; Replace = 'return changes;'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'return RegionsOnly(root, checkedEnd, moved, lines[insertLine].Start) ? changes : null;'; Replace = 'return changes;'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'if (below > endLine && below < lines.Count && !IsBlank(lines[below]))'; Replace = 'if (false)'; Tests = 'UsingPlacementTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'; Find = 'return UsingPlacementMode.Preserve;'; Replace = 'return UsingPlacementMode.Inside;'; Tests = 'UsingPlacementTests' }

        # BRO1527 (auto-accessors on one line)
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken)'; Replace = 'false'; Tests = 'FixOrderTests' }

        # BRO1001 stylebro_keep_overloads_together (Sonar S4136)
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (options.KeepOverloadsTogether)'; Replace = 'if (true)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = ' || keys[i].CompareTo(keys[anchor]) < 0))'; Replace = '))'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '? (segments[i], method.'; Replace = '? (0, method.'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (anchors is not null && anchors[a] != anchors[b])'; Replace = 'if (false)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '.ThenBy(i => anchors[i]).'; Replace = '.'; Tests = 'MemberOrderingTests' }

        # BRO1505: a field below a field that spans several lines; BRO1001's sort and BRO1114's split add that blank line
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'return Layout.ElementSeparation.NeedsBlankLine(previous, current, previous.SyntaxTree.GetText(), autoAccessorLines, allowAdjacentSingleLine);'; Replace = 'return !(previous is FieldDeclarationSyntax && current is FieldDeclarationSyntax);'; Tests = 'FixOrderTests' }

        # BRO1001: comments that introduce a group (GroupComments)
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = '|| GroupComments.MovesGroupComment(members, keys, order)'; Replace = ''; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = 'if (keys[i + 1].Kind == keys[i].Kind && '; Replace = 'if ('; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = '&& Moves(position, i) && HasComment'; Replace = '&& HasComment'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = 'if (trivia.IsDirective)'; Replace = 'if (false)'; Tests = 'MemberOrderingTests' }

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

        # Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = '= await ApplyLayoutAsync(document, root, new HashSet<SyntaxNode>(partialAccess.Keys), cancellationToken).ConfigureAwait(false);'; Replace = '= (document, root, partialAccess.Keys.ToDictionary(k => k, k => k));'; Tests = 'FixOrderTests.Regions_RemovedAroundMembersMissingBlankLines' }

        # Options for declarations (2026-10-07): BRO1601 inheritdoc style, BRO1505 adjacent single-line members,
        # BRO1105 / BRO1111 same_line
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'StyleBro.Analyzers.Layout.ElementSeparation.AllowsAdjacentSingleLineMembers(document'; Replace = 'false && StyleBro.Analyzers.Layout.ElementSeparation.AllowsAdjacentSingleLineMembers(document'; Tests = 'MemberOrderingTests' }

        # Fix order: BRO1001 makes BRO1509's expansion and BRO1505's blank lines before sorting; a one-line type stays on one line
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'if ((own.Count > 0 || IsOn(DiagnosticIds.SingleLineElement))'; Replace = 'if ((own.Count > 0)'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'if ((own.Count > 0 || IsOn(DiagnosticIds.SingleLineElement))'; Replace = 'if ((IsOn(DiagnosticIds.SingleLineElement))'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'var violations = IsOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'var violations = false'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = 'var violations = IsOn(DiagnosticIds.ElementsSeparatedByBlankLine)'; Replace = 'var violations = true'; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'; Find = '.Sum(c => c.NewText!.Length - c.Span.Length);'; Replace = '.Sum(c => 0);'; Tests = 'FixOrderTests.MemberOrder_BlankLinesInOtherSlots' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (slot > 0 && !oneLine && '; Replace = 'if (slot > 0 && '; Tests = 'MemberOrderingTests' }
        @{ File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'; Find = 'if (slot < count - 1 && !oneLine && '; Replace = 'if (slot < count - 1 && '; Tests = 'MemberOrderingTests' }

        # A comment below code (BRO1504, BRO1506, BRO1001; eShop)
        @{ File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'; Find = 'members[i].GetLeadingTrivia().Any(t => Layout.BlankLines.IsCommentBelowCode(t, members[i].SyntaxTree.GetText())) && position[i] != position[i - 1] + 1'; Replace = 'false'; Tests = 'MemberOrderingTests.ACommentBelowAMember_KeepsTheContainerAsItIs' }
    )
}
