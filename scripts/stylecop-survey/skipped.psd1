# Every StyleCop rule StyleBro doesn't cover (neither a StyleBro rule nor an SDK setting the preset turns on), with why
# and what it would take to revisit it. New-Mapping.ps1 turns this into docs/skipped-rules.md and uses it for the
# proposal column of docs/stylecop-mapping.md; it warns when an uncovered rule has no entry here.
#
# Status:
#   Candidate      A StyleBro rule with a safe fix looks feasible; not done yet.
#   SdkLater       The SDK can fix it (measured by Test-SdkCoverage.ps1), but the preset and stylebro-migrate don't
#                  turn that on yet (see Why for the catch).
#   Drop           No safe automatic fix (the fix needs human-written text, an API change, or file operations).
#   NotInStyleCop  StyleCop 1.2 never reports it (probed with an obvious violation in sdk-check-cases.ps1).
#   Variant        A variant id of another rule.
#   NotApplicable  Not a code rule.
#
# Partial: rules that ARE covered, but with known differences worth revisiting.
@{
    Skipped = @{
        # ---- Special -----------------------------------------------------------------------------------------------
        SA0001 = @{ Status = 'NotApplicable'; Why = 'StyleCop''s own setup diagnostic: XML comment analysis is disabled (no GenerateDocumentationFile).'
            Revisit = 'Nothing to port. stylebro-migrate already detects the same condition and keeps the XML documentation rules (BRO1603-BRO1611) off.' }
        SA0002 = @{ Status = 'NotApplicable'; Why = 'StyleCop''s own diagnostic for an invalid stylecop.json.'
            Revisit = 'Nothing to port. stylebro-migrate parses stylecop.json leniently (comments, trailing commas).' }

        # ---- Spacing -----------------------------------------------------------------------------------------------
        SA1004 = @{ Status = 'Candidate'; Why = 'Not done yet. `///text` -> `/// text`; the SDK doesn''t fix it (measured).'
            Revisit = 'Text-based like BRO1002 (SA1005), which already skips `///`. Check whether StyleCop also reports several spaces (indentation inside `<code>` must stay). Small.' }
        SA1006 = @{ Status = 'Candidate'; Why = 'Not done yet. `# if` -> `#if`; the SDK doesn''t fix it (measured). Rare in practice.'
            Revisit = 'Text edit inside the directive trivia. Small, low value.' }

        # ---- Readability -------------------------------------------------------------------------------------------
        SA1100 = @{ Status = 'Candidate'; Why = 'Not done yet, and riskier than it looks: `base.M()` -> `this.M()` turns a non-virtual call into a virtual one, so a derived class that overrides M changes behavior.'
            Revisit = 'Needs the semantic model; only safe when M isn''t virtual/overridable or the type is sealed. Decide whether to report the unsafe cases without a fix (design rule 3 says skip).' }
        SA1102 = @{ Status = 'Candidate'; Why = 'Not done yet. LINQ query clause layout (no blank line between clauses). 0 findings in the surveyed repos.'
            Revisit = 'Layout fix like BRO1107/BRO1108 (ParameterLayout) for query clauses; SA1102-SA1105 belong together. Low demand.' }
        SA1103 = @{ Status = 'Candidate'; Why = 'Not done yet. Query clauses all on one line or each on its own (like SA1117 for queries).'
            Revisit = 'Same approach as BRO1108; do SA1102-SA1105 as one batch.' }
        SA1104 = @{ Status = 'Candidate'; Why = 'Not done yet. A clause after a multi-line clause starts on a new line.'
            Revisit = 'Part of the SA1102-SA1105 batch.' }
        SA1105 = @{ Status = 'Candidate'; Why = 'Not done yet. A multi-line clause starts on its own line.'
            Revisit = 'Part of the SA1102-SA1105 batch.' }
        SA1108 = @{ Status = 'Drop'; Why = 'StyleCop has no fix. A comment between `if (x)` and `{` would have to move, and there is no right place for it automatically (above the statement or inside the block change its meaning).'
            Revisit = 'Possible rule: move it to the line above the statement. Probe what teams expect first.' }
        SA1109 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic for a region between `if (b)` and its block (probed).'
            Revisit = 'Nothing to port. Regions are BRO1112/BRO1113.' }
        SA1112 = @{ Status = 'Candidate'; Why = 'Not done yet. `M(` + newline + `)` -> `M()`; the SDK doesn''t fix it (measured).'
            Revisit = 'Extend ParenthesisPlacement (BRO1110 skips empty lists on purpose; this is the empty-list case). Small.' }
        SA1113 = @{ Status = 'Candidate'; Why = 'Not done yet. A comma at the start of the next line moves to the end of the previous item.'
            Revisit = 'Gap rewrite in ParameterLayout (same lists as BRO1107); keep comments in the gap out (skip).' }
        SA1114 = @{ Status = 'Candidate'; Why = 'StyleCop has no fix. No blank line or comment between `(` and the first parameter.'
            Revisit = 'Delete the blank lines (comments: skip). Same list kinds as BRO1107. Small.' }
        SA1115 = @{ Status = 'Candidate'; Why = 'StyleCop has no fix. No blank line between a comma and the next parameter.'
            Revisit = 'Delete the blank lines (comments: skip). Do together with SA1114.' }
        SA1118 = @{ Status = 'Drop'; Why = 'StyleCop has no fix. A multi-line argument (other than the first, or a lambda/anonymous object) needs extracting into a variable: a refactoring that names things. 126 findings in the private app.'
            Revisit = 'Only if a deterministic extraction is acceptable (variable name from the parameter name). Probably stays a human job.' }
        SA1120 = @{ Status = 'Candidate'; Why = 'Not done yet. An empty `//` comment line is removed.'
            Revisit = 'Text-based; coordinate with BRO1002, which turns whitespace-only comments into `//` (that would then be removed). Small.' }
        SA1125 = @{ Status = 'Candidate'; Why = 'StyleCop has no fix. `Nullable<int>` -> `int?`; no SDK rule covers it.'
            Revisit = 'Semantic model to confirm System.Nullable<T>; skip `typeof(Nullable<>)` and cref. Small.' }
        SA1126 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic on an unprefixed member call (probed).'
            Revisit = 'Nothing to port.' }
        SA1130 = @{ Status = 'Candidate'; Why = 'Not done yet. `delegate (int x) { ... }` -> `(int x) => { ... }`.'
            Revisit = '`delegate { }` without a parameter list can bind to any delegate type; converting it needs the target''s parameters (semantic model) and may need discards. Medium.' }
        SA1132 = @{ Status = 'Candidate'; Why = 'Not done yet. `int a, b;` -> one field per declaration.'
            Revisit = 'Like BRO1102 (attributes and doc comments must be duplicated or kept on the first). Mind initializer order (InitializerOrder.cs). Small-medium.' }
        SA1135 = @{ Status = 'Candidate'; Why = 'Not done yet. Usings inside a namespace must be fully qualified.'
            Revisit = 'Only relevant to teams with usings inside the namespace (SA1200 default). Semantic model to get the full name. Small.' }
        SA1136 = @{ Status = 'Candidate'; Why = 'Not done yet. Each enum member on its own line.'
            Revisit = 'Layout fix; reuse ParameterLayout''s "each item on its own line". Small.' }
        SA1139 = @{ Status = 'Candidate'; Why = 'Not done yet. `(long)1` -> `1L`.'
            Revisit = 'Semantic model for the constant and target type; only numeric literals, skip checked/unchecked edge cases. Small.' }
        SA1141 = @{ Status = 'Candidate'; Why = 'Not done yet (StyleCop 1.2 only). `ValueTuple<int, string>` -> `(int, string)`.'
            Revisit = 'Semantic model; skip ValueTuple with more than 7 elements (TRest) and `typeof`. Small.' }
        SA1142 = @{ Status = 'Candidate'; Why = 'Not done yet (StyleCop 1.2 only). `t.Item1` -> `t.Name` when the element has a name.'
            Revisit = 'Semantic model; the name must be the same in every use (inferred names differ by C# version). Small.' }

        # ---- Ordering ----------------------------------------------------------------------------------------------
        SA1205 = @{ Status = 'Candidate'; Why = 'Not done yet. A partial type part without an access modifier gets the one the other part declares (or the default).'
            Revisit = 'Check first whether IDE0040 already does it with `dotnet_style_require_accessibility_modifiers` (not in the SDK check). Semantic model otherwise. Small.' }
        SA1206 = @{ Status = 'SdkLater'; Why = 'The SDK''s IDE0036 (`csharp_preferred_modifier_order`) fixes it (measured), but IDE0036 enforces a complete order while StyleCop only wants access modifier, then `static`, then the rest in any order: IDE0036 would also reorder `async override` and other combinations StyleCop accepts.'
            Revisit = 'Measure IDE0036 on the StyleCop-clean repos (Measure-MigrationDelta.ps1). If the extra reorders are rare, add IDE0036 to the preset and to stylebro-migrate; otherwise write a BRO rule that only moves access modifiers and `static` forward.' }
        SA1207 = @{ Status = 'SdkLater'; Why = 'IDE0036 fixes `internal protected` -> `protected internal` (measured); same catch as SA1206.'
            Revisit = 'Decide together with SA1206.' }
        SA1212 = @{ Status = 'Candidate'; Why = 'Not done yet. `set` before `get` -> swap the accessors.'
            Revisit = 'Swap the accessor declarations with their trivia (like BRO1001 does for members). Small.' }
        SA1213 = @{ Status = 'Candidate'; Why = 'Not done yet. `remove` before `add` -> swap the accessors.'
            Revisit = 'Same code as SA1212.' }

        # ---- Naming ------------------------------------------------------------------------------------------------
        SA1301 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic (probed). StyleCop keeps the id for compatibility.'
            Revisit = 'Nothing to port.' }
        SA1305 = @{ Status = 'Drop'; Why = 'Off by default, StyleCop has no fix. The fix would have to guess the name without the prefix (`strName` -> `name`, but `isOpen` is fine), configured by allowed prefixes. 82 / 1 / 14 findings with the rule on.'
            Revisit = 'Possible with the BRO13xx renamer if the prefix list is explicit (`stylebro_hungarian_prefixes`); rename guards apply. Medium.' }
        SA1316 = @{ Status = 'Candidate'; Why = 'Not done yet (StyleCop 1.2 only). Tuple element names in PascalCase (configurable: tupleElementNameCasing).'
            Revisit = 'Tuple element names aren''t symbols SymbolFinder renames; every use (deconstruction, `t.name`, inferred names) must change together. Medium, risky.' }

        # ---- Maintainability ---------------------------------------------------------------------------------------
        SA1401 = @{ Status = 'Drop'; Why = 'Making a public or protected field private changes the public API and breaks callers. 141 / 48 / 39 findings with the rule on.'
            Revisit = 'Only for private nested types or `internal` fields without InternalsVisibleTo, converting to a property. Probably not worth it.' }
        SA1402 = @{ Status = 'Drop'; Why = 'Moving a type to its own file isn''t something `dotnet format` can do (code fixes can''t add documents through it).'
            Revisit = 'Would need a separate tool (stylebro-migrate could do it as a one-off).' }
        SA1403 = @{ Status = 'Drop'; Why = 'StyleCop has no fix; splitting namespaces into files has the same problem as SA1402.'
            Revisit = 'Same as SA1402.' }
        SA1404 = @{ Status = 'Drop'; Why = 'The justification of a SuppressMessage has to be written by a person.'
            Revisit = 'Nothing automatic.' }
        SA1405 = @{ Status = 'Drop'; Why = 'StyleCop has no fix; the message for Debug.Assert has to be written by a person.'
            Revisit = 'A generated message (the condition''s text, like CallerArgumentExpression) is possible but adds little.' }
        SA1406 = @{ Status = 'Drop'; Why = 'StyleCop has no fix; the message for Debug.Fail has to be written by a person.'
            Revisit = 'Nothing automatic.' }
        SA1409 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic for an empty `try { } finally { }` (probed).'
            Revisit = 'Nothing to port.' }
        SA1410 = @{ Status = 'Candidate'; Why = 'Not done yet. `delegate() { }` -> `delegate { }`.'
            Revisit = 'Only when the empty parameter list isn''t needed for overload resolution (semantic model). Small; consider doing with SA1130.' }
        SA1411 = @{ Status = 'Candidate'; Why = 'Not done yet. `[Obsolete()]` -> `[Obsolete]`.'
            Revisit = 'Syntax-only. Small.' }
        SA1414 = @{ Status = 'Drop'; Why = 'StyleCop has no fix (1.2 only); tuple element names in signatures have to be chosen by a person.'
            Revisit = 'Nothing automatic.' }

        # ---- Layout ------------------------------------------------------------------------------------------------
        SA1501 = @{ Status = 'Candidate'; Why = 'The SDK''s `csharp_preserve_single_line_blocks = false` fixes it (measured) but also expands every auto-property''s `{ get; set; }`, which StyleCop allows, so the preset keeps single-line blocks.'
            Revisit = 'BRO rule: expand single-line statement blocks (`if (x) { return; }`) only. Do with SA1502. Medium (indentation from .editorconfig, like BRO1105).' }
        SA1502 = @{ Status = 'Candidate'; Why = 'Same SDK problem as SA1501. 356 findings in the private app (which has it off).'
            Revisit = 'BRO rule: expand single-line types, methods, and accessors with bodies; leave `{ get; set; }` and empty-bodied auto-properties alone, like StyleCop.' }
        SA1504 = @{ Status = 'Candidate'; Why = 'Not done yet. All accessors of a property single-line or all multi-line.'
            Revisit = 'Expand the single-line ones (StyleCop''s fix). Overlaps with SA1502; do together. Small-medium.' }
        SA1506 = @{ Status = 'Candidate'; Why = 'Not done yet. No blank line between a doc comment and its element.'
            Revisit = 'Delete the blank lines; the BlankLineAfter code (BRO1503/BRO1506) has the line logic. Small.' }
        SA1511 = @{ Status = 'Candidate'; Why = 'Not done yet. No blank line before the `while` of a `do ... while`.'
            Revisit = 'Add `while` after `do` to BRO1502''s keywords (it''s left out on purpose so far). Small.' }
        SA1514 = @{ Status = 'Candidate'; Why = 'Not done yet. A doc comment is preceded by a blank line (unless it follows `{`).'
            Revisit = 'Mostly covered by BRO1505 (element separation inserts the blank line above the doc comment); check the remaining cases with the parity script. Small.' }

        # ---- Documentation -----------------------------------------------------------------------------------------
        SA1601 = @{ Status = 'Drop'; Why = 'Missing documentation on a partial element; the only fix is placeholder text (no stubs, user decision).'
            Revisit = 'Revisit with the stub decision.' }
        SA1602 = @{ Status = 'Drop'; Why = 'Missing documentation on enum members; the only fix is placeholder text (no stubs, user decision). 530 findings in the private app.'
            Revisit = 'Revisit with the stub decision.' }
        SA1603 = @{ Status = 'Drop'; Why = 'Off by default, no StyleCop fix; invalid XML needs a person to say what was meant.'
            Revisit = 'A narrow fix is possible: escape a lone `&` or `<` in text. Probe how often that''s the cause.' }
        SA1604 = @{ Status = 'Drop'; Why = 'A doc comment without `<summary>`; the fix would be placeholder text.'
            Revisit = 'Revisit with the stub decision. If the comment has plain text outside any tag, wrapping it in `<summary>` is a real fix: worth a probe.' }
        SA1605 = @{ Status = 'Drop'; Why = 'Same as SA1604 for partial elements.'
            Revisit = 'With SA1604.' }
        SA1606 = @{ Status = 'Drop'; Why = 'An empty `<summary>`; the fix would be placeholder text (no stubs, user decision).'
            Revisit = 'Revisit with the stub decision.' }
        SA1607 = @{ Status = 'Drop'; Why = 'Same as SA1606 for partial elements.'
            Revisit = 'With SA1606.' }
        SA1608 = @{ Status = 'Drop'; Why = 'The default "Summary description for X" text has to be replaced by a person.'
            Revisit = 'Nothing automatic.' }
        SA1609 = @{ Status = 'Drop'; Why = 'Off by default. Missing `<value>`; the fix would be placeholder text (no stubs, user decision).'
            Revisit = 'Revisit with the stub decision. A `<value>` derived from the summary ("Gets the name." -> "The name.") is conceivable.' }
        SA1610 = @{ Status = 'Drop'; Why = 'An empty `<value>`; StyleCop''s fix writes placeholder text.'
            Revisit = 'Removing the empty tag would be a real fix, but trades one diagnostic for SA1609 (off by default). Decide with SA1609.' }
        SA1611 = @{ Status = 'Drop'; Why = 'Missing `<param>`; the fix would be placeholder text (no stubs, user decision).'
            Revisit = 'Revisit with the stub decision.' }
        SA1613 = @{ Status = 'Candidate'; Why = 'Not done yet. A `<param>` without `name`: when exactly one parameter is undocumented, the name is known.'
            Revisit = 'Extend BRO1611''s finding (it already renames a stale tag when exactly one parameter is undocumented). Otherwise skip. Small.' }
        SA1614 = @{ Status = 'Drop'; Why = 'An empty `<param>`; the fix would be placeholder text.'
            Revisit = 'Revisit with the stub decision.' }
        SA1615 = @{ Status = 'Drop'; Why = 'Missing `<returns>`; the fix would be placeholder text.'
            Revisit = 'Revisit with the stub decision.' }
        SA1616 = @{ Status = 'Drop'; Why = 'An empty `<returns>`; the fix would be placeholder text.'
            Revisit = 'Revisit with the stub decision.' }
        SA1618 = @{ Status = 'Drop'; Why = 'Missing `<typeparam>`; the fix would be placeholder text.'
            Revisit = 'Revisit with the stub decision.' }
        SA1619 = @{ Status = 'Drop'; Why = 'Same as SA1618 for partial types.'
            Revisit = 'With SA1618.' }
        SA1620 = @{ Status = 'Candidate'; Why = 'Not done yet. `<typeparam>` tags match the type parameters: the same as BRO1611 for `<param>`.'
            Revisit = 'Generalize ParameterDocumentation (rename a stale tag, remove, reorder) to type parameters. Check what StyleCop skips (BRO1611 found it skips constructors/operators). Small-medium.' }
        SA1621 = @{ Status = 'Candidate'; Why = 'Not done yet. A `<typeparam>` without `name`.'
            Revisit = 'Same as SA1613, for type parameters.' }
        SA1622 = @{ Status = 'Drop'; Why = 'An empty `<typeparam>`; the fix would be placeholder text.'
            Revisit = 'Revisit with the stub decision.' }
        SA1625 = @{ Status = 'Drop'; Why = 'Identical text copied between tags has to be rewritten by a person.'
            Revisit = 'Nothing automatic.' }
        SA1628 = @{ Status = 'NotInStyleCop'; Why = 'StyleCop 1.2 never reports it (not implemented, probed while building BRO16xx).'
            Revisit = 'Nothing to port. A capitalization fix would be easy if a team wants it.' }
        SA1630 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic on a one-word summary (probed).'
            Revisit = 'Nothing to port.' }
        SA1631 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic on a summary of symbols (probed).'
            Revisit = 'Nothing to port.' }
        SA1632 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic on a one-letter summary (probed).'
            Revisit = 'Nothing to port.' }
        SA1634 = @{ Status = 'Candidate'; Why = 'StyleCop''s XML file header (`// <copyright file="X.cs" company="Y">`) has no SDK equivalent; IDE0073 only writes a plain header (SA1633 with `xmlHeader: false` is covered).'
            Revisit = 'One BRO rule for SA1634-SA1641 that writes/corrects the XML header from stylecop.json''s documentationRules (companyName, copyrightText, headerDecoration, variables). Medium; worth it for teams on the default XML header.' }
        SA1635 = @{ Status = 'Candidate'; Why = 'XML file header: copyright text missing. See SA1634.'
            Revisit = 'With SA1634.' }
        SA1636 = @{ Status = 'Candidate'; Why = 'XML file header: copyright text doesn''t match stylecop.json. See SA1634.'
            Revisit = 'With SA1634.' }
        SA1637 = @{ Status = 'Candidate'; Why = 'XML file header: file name missing. See SA1634.'
            Revisit = 'With SA1634.' }
        SA1638 = @{ Status = 'Candidate'; Why = 'XML file header: file name doesn''t match the file. See SA1634.'
            Revisit = 'With SA1634. Linked files (multi-targeting) have one physical name, so this is safe.' }
        SA1639 = @{ Status = 'Drop'; Why = 'Off by default; the header''s `<summary>` has to be written by a person.'
            Revisit = 'Nothing automatic.' }
        SA1640 = @{ Status = 'Candidate'; Why = 'XML file header: company attribute missing. See SA1634.'
            Revisit = 'With SA1634.' }
        SA1641 = @{ Status = 'Candidate'; Why = 'XML file header: company doesn''t match stylecop.json. See SA1634.'
            Revisit = 'With SA1634.' }
        SA1644 = @{ Status = 'NotInStyleCop'; Why = 'StyleCop 1.2 never reports it (not implemented, probed while building BRO16xx).'
            Revisit = 'Nothing to port.' }
        SA1645 = @{ Status = 'Drop'; Why = 'Off by default; a missing `<include>` file can''t be fixed automatically.'
            Revisit = 'Nothing automatic.' }
        SA1646 = @{ Status = 'Drop'; Why = 'Off by default; a wrong `<include>` XPath can''t be fixed automatically.'
            Revisit = 'Nothing automatic.' }
        SA1647 = @{ Status = 'Drop'; Why = 'Off by default; an `<include>` without file/path can''t be fixed automatically.'
            Revisit = 'Nothing automatic.' }
        SA1648 = @{ Status = 'Drop'; Why = '`<inheritdoc/>` where there is nothing to inherit: the fix is real documentation.'
            Revisit = 'Nothing automatic. (BRO1601 only adds `<inheritdoc/>` where there is something to inherit.)' }
        SA1649 = @{ Status = 'Drop'; Why = 'Renaming files isn''t something `dotnet format` can do.'
            Revisit = 'Would need a separate tool (stylebro-migrate could do it as a one-off, with `git mv`).' }
        SA1650 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic on misspelled words (probed).'
            Revisit = 'Nothing to port.' }

        # ---- Variants ----------------------------------------------------------------------------------------------
        SA1119_p = @{ Status = 'Variant'; Why = 'SA1119 for parenthesized patterns (1.2 only), covered with SA1119 by IDE0047.'
            Revisit = 'Nothing separate to do.' }
        SX1309S = @{ Status = 'Variant'; Why = 'Static fields begin with `_`. BRO1303 with `_camelCase` (which stylebro-migrate sets for SX1309) covers private static non-readonly fields; `static readonly` fields are BRO1306''s (PascalCase), so a team using `_` for those has no StyleBro rule.'
            Revisit = 'With SX1309; decide whether `_camelCase` should extend to private static readonly fields.' }
    }

    Partial = @{
        SA1119 = @{ Why = 'Covered by the SDK''s IDE0047, which is a little broader: it also removes the parentheses in `a ?? (b ?? c)`, which SA1119 accepts (2 lines in Polly after migrating).'
            Revisit = 'Nothing to configure in IDE0047. Only a BRO replacement for SA1119 would match exactly; not worth it so far.' }
        SA1407 = @{ Why = 'Covered by IDE0048, but one `dotnet format` pass fixes only some cases (the SDK check fixes SA1407 or SA1408, varying between runs).'
            Revisit = 'Report to dotnet/format, or document "run twice" for the first run.' }
        SA1408 = @{ Why = 'Same as SA1407 (IDE0048).'
            Revisit = 'With SA1407.' }
        SA1507 = @{ Why = 'Covered by the SDK''s IDE2000, which also counts blank lines at the start of a file and right before `}`; StyleCop leaves those to SA1517/SA1508 (9 files in the private app, which has those off).'
            Revisit = 'Nothing to configure in IDE2000.' }
        SA1600 = @{ Why = 'BRO1601 adds `<inheritdoc/>` to overrides and implementations; other missing documentation isn''t reported (no stubs, user decision).'
            Revisit = 'Revisit with the stub decision (also SA1601, SA1602, SA1604-SA1607, SA1609-SA1611, SA1614-SA1616, SA1618, SA1619, SA1622).' }
        SA1633 = @{ Why = 'IDE0073 writes a plain header from `file_header_template`; StyleCop''s default XML header isn''t migrated (stylebro-migrate notes it).'
            Revisit = 'With the SA1634-SA1641 header rule.' }
    }
}
