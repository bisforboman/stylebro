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

        # ---- Readability -------------------------------------------------------------------------------------------
        SA1109 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic for a region between `if (b)` and its block (probed).'
            Revisit = 'Nothing to port. Regions are BRO1112/BRO1113.' }
        SA1118 = @{ Status = 'Drop'; Why = 'StyleCop has no fix. A multi-line argument (other than the first, or a lambda/anonymous object) needs extracting into a variable: a refactoring that names things. 126 findings in the private app.'
            Revisit = 'Only if a deterministic extraction is acceptable (variable name from the parameter name). Probably stays a human job.' }
        SA1126 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic on an unprefixed member call (probed).'
            Revisit = 'Nothing to port.' }

        # ---- Ordering ----------------------------------------------------------------------------------------------

        # ---- Naming ------------------------------------------------------------------------------------------------
        SA1301 = @{ Status = 'NotInStyleCop'; Why = 'Off by default and no diagnostic (probed). StyleCop keeps the id for compatibility.'
            Revisit = 'Nothing to port.' }

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
        SA1414 = @{ Status = 'Drop'; Why = 'StyleCop has no fix (1.2 only); tuple element names in signatures have to be chosen by a person.'
            Revisit = 'Nothing automatic.' }

        # ---- Layout ------------------------------------------------------------------------------------------------

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
        SA1639 = @{ Status = 'Drop'; Why = 'Off by default; the header''s `<summary>` has to be written by a person.'
            Revisit = 'Nothing automatic.' }
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
        SA1119_p = @{ Status = 'Variant'; Why = 'Not a rule of its own: a Hidden diagnostic SA1119 reports on the `(` and `)` tokens so the IDE greys them out (StyleCop''s ParenthesisDescriptor, read in its source). BRO1405 covers SA1119 itself.'
            Revisit = 'Nothing to port (an earlier note here took it for parenthesized patterns; StyleCop doesn''t check those).' }
        SX1309S = @{ Status = 'Variant'; Why = 'Static fields begin with `_`. BRO1303 with `_camelCase` (which stylebro-migrate sets for SX1309) covers private static non-readonly fields; `static readonly` fields are BRO1306''s (PascalCase), so a team using `_` for those has no StyleBro rule.'
            Revisit = 'With SX1309; decide whether `_camelCase` should extend to private static readonly fields.' }
    }

    Partial = @{
        SA1206 = @{ Why = 'IDE0036 (`csharp_preferred_modifier_order`) enforces the whole modifier order, while StyleCop only wants the access modifier first and `static` next. Measured on six repos (2026-10-02): IDE0036 reordered nothing that StyleCop accepts (Polly, OpenTelemetry, the private app, FFMpegCore, Serilog 0; Newtonsoft.Json 27 lines, all SA1206 violations such as `public new static`).'
            Revisit = 'Only if a team reports reorders StyleCop accepted (`async override` vs `override async`); then a BRO rule that only moves access modifiers and `static`.' }
        SA1600 = @{ Why = 'BRO1601 adds `<inheritdoc/>` to overrides and implementations; other missing documentation isn''t reported (no stubs, user decision).'
            Revisit = 'Revisit with the stub decision (also SA1601, SA1602, SA1604-SA1607, SA1609-SA1611, SA1614-SA1616, SA1618, SA1619, SA1622).' }
    }
}
