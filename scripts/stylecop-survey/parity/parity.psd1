# Sets for Compare-WithStyleCop.ps1. Each folder holds case files; Map pairs each StyleCop rule with its StyleBro
# rule. Expected lists every difference the comparison is allowed to find: StyleBro's documented deviations
# (see docs/rules). Output lines are shown untrimmed, in brackets.
@{
    Sets = @(
        @{
            Name     = 'namespace-order'
            Map      = @('SA1201=BRO1001', 'SA1202=BRO1001', 'SA1204=BRO1001')
            Expected = @(
                # BRO1001 reports once per container (the first out-of-place element); the fix sorts all of it.
                'only StyleCop: BRO1001 Types.cs(11,17)'
                'only StyleCop: BRO1001 Types.cs(20,26)'
                'only StyleCop: BRO1001 Types.cs(30,15)'
            )
        }
        @{
            Name     = 'member-order-regions'
            Map      = @('SA1201=BRO1001', 'SA1202=BRO1001', 'SA1203=BRO1001', 'SA1204=BRO1001', 'SA1214=BRO1001')
            # StyleCop's fix moves members across regions; positions only.
            CompareOutput = $false
            Expected = @(
                # BRO1001 reports once per type (the first out-of-place member); the fix sorts every region.
                'only StyleCop: BRO1001 Regions.cs(14,17)'
                # The order across regions isn't checked: the fix would have to move a member into another region.
                'only StyleCop: BRO1001 Regions.cs(27,17)'
            )
        }
        @{
            Name     = 'blank-line-runs'
            Map      = @('SA1507=BRO1517', 'SA1508=BRO1518', 'SA1513=BRO1519')
            Expected = @(
                # BRO1519 leaves a comment after '}' to BRO1504 and the next member to BRO1505 (they add the same line).
                'only StyleCop: BRO1519 Blank.cs(45,14)'
                'only StyleCop: BRO1519 Blank.cs(49,10)'
                'StyleCop output only: Blank.cs: []'
                'StyleCop output only: Blank.cs: []'
            )
        }
        @{
            Name     = 'precedence'
            Map      = @('SA1407=BRO1406', 'SA1408=BRO1407')
            Expected = @(
                # BRO1407 also checks 'and'/'or' patterns, like StyleCop's current source (added after 1.2.0-beta.556).
                'only StyleBro: BRO1407 Precedence.cs(18,41)'
                'only StyleBro: BRO1407 Precedence.cs(23,38)'
                'StyleBro output only: Precedence.cs: [            var p = a || (b && c) || (v is (> 1 and < 5) or 10 && a);]'
                'StyleBro output only: Precedence.cs: [        public bool P(int v) => v is (> 1 and < 5) or 10;]'
                'StyleCop output only: Precedence.cs: [            var p = a || (b && c) || (v is > 1 and < 5 or 10 && a);]'
                'StyleCop output only: Precedence.cs: [        public bool P(int v) => v is > 1 and < 5 or 10;]'
            )
        }
        @{
            Name     = 'parentheses'
            Map      = @('SA1119=BRO1405')
            Expected = @(
                # Skipped: removing them turns the arguments into a generic method call (StyleCop's fix doesn't compile).
                'only StyleCop: BRO1405 Parens.cs(25,24)'
                'StyleBro output only: Parens.cs: [            Use(a < b, (b > (a + 1)));]'
                'StyleCop output only: Parens.cs: [            Use(a < b, b > (a + 1));]'
                'StyleCop fix doesn''t compile: Parens.cs(25,17) CS0307'
                'StyleCop fix doesn''t compile: Parens.cs(25,21) CS0118'
                'StyleCop fix doesn''t compile: Parens.cs(25,24) CS0118'
                # StyleCop's fix leaves the spaces inside '( b )'.
                'StyleBro output only: Parens.cs: [            Use(a + b, b);]'
                'StyleCop output only: Parens.cs: [            Use(a + b,  b );]'
            )
        }
        @{
            Name     = 'access-modifiers'
            Map      = @('SA1400=BRO1404', 'SA1205=BRO1007')
            Expected = @()
        }
        @{
            Name     = 'braces'
            Map      = @('SA1503=BRO1514', 'SA1519=BRO1515', 'SA1520=BRO1516')
            Expected = @(
                # Skipped: a comment where the brace would go, a line break inside a token (multi-line string).
                'only StyleCop: BRO1514 Braces.cs(75,17)'
                'only StyleCop: BRO1515 Braces.cs(78,17)'
                'StyleCop output only: Braces.cs: [            {]'
                'StyleCop output only: Braces.cs: [            }]'
                'StyleCop output only: Braces.cs: [            {]'
                'StyleCop output only: Braces.cs: [            }]'
            )
        }
        @{
            Name     = 'embedded-comments'
            Map      = @('SA1108=BRO1132')
            # StyleCop has no fix for SA1108; positions only.
            CompareOutput = $false
            Expected = @(
                # Skipped: a single-line block (no line inside it to move the comment to), a comment spanning lines.
                'only StyleCop: BRO1132 Comments.cs(155,20)'
                'only StyleCop: BRO1132 Comments.cs(158,20)'
                # Skipped (user decision): a header spanning several lines; the comment explains its last line.
                'only StyleCop: BRO1132 Comments.cs(164,24)'
                'only StyleCop: BRO1132 Comments.cs(168,23)'
            )
        }
        @{
            Name     = 'base-calls'
            Map      = @('SA1100=BRO1131')
            Expected = @(
                # BRO1131: virtual members in a type that isn't sealed are skipped ('this.' dispatches virtually).
                'only StyleCop: BRO1131 BaseCalls.cs(26,32)'
                'StyleBro output only: BaseCalls.cs: [        public void Clear() => base.Reset();]'
                'StyleCop output only: BaseCalls.cs: [        public void Clear() => this.Reset();]'
            )
        }
        @{
            Name     = 'directive-spacing'
            Map      = @('SA1006=BRO1006')
            Expected = @()
        }
        @{
            Name     = 'empty-statements-attributes'
            Map      = @('SA1106=BRO1101', 'SA1133=BRO1102')
            Expected = @(
                # BRO1101: embedded empty statements (if/while/for) are left to the compiler's CS0642.
                'only StyleCop: BRO1101 Empty.cs(10,20)'
                'only StyleCop: BRO1101 Empty.cs(11,23)'
                'only StyleCop: BRO1101 Empty.cs(12,22)'
                'StyleBro output only: Empty.cs: [            if (b) ;]'
                'StyleBro output only: Empty.cs: [            while (b) ;]'
                'StyleBro output only: Empty.cs: [            for (;;) ;]'
                'StyleCop output only: Empty.cs: [            if (b)]'
                'StyleCop output only: Empty.cs: [            while (b)]'
                'StyleCop output only: Empty.cs: [            for (;;)]'
                'StyleCop output only: Empty.cs: [            {]'
                'StyleCop output only: Empty.cs: [            {]'
                'StyleCop output only: Empty.cs: [            {]'
                'StyleCop output only: Empty.cs: [            }]'
                'StyleCop output only: Empty.cs: [            }]'
                'StyleCop output only: Empty.cs: [            }]'
                # BRO1101: labeled empty statements are left alone (removing the ';' can break the build).
                'only StyleCop: BRO1101 Empty.cs(13,20)'
                'StyleBro output only: Empty.cs: [            label: ;]'
                'StyleCop output only: Empty.cs: [        label:]'
                # BRO1101: '; // comment' keeps the comment's indentation; StyleCop's fix leaves one space.
                'StyleBro output only: Empty.cs: [            // trailing comment]'
                'StyleCop output only: Empty.cs: [ // trailing comment]'
                # BRO1102: a list with a comment between attributes is skipped; StyleCop's fix drops the comment.
                'only StyleCop: BRO1102 Attrs.cs(18,38)'
                'StyleBro output only: Attrs.cs: [        [Obsolete("a, b"), /* why */ DebuggerStepThrough]]'
                'StyleCop output only: Attrs.cs: [        [Obsolete("a, b")]]'
                'StyleCop output only: Attrs.cs: [        [DebuggerStepThrough]]'
            )
        }
        @{
            Name     = 'blank-lines'
            Map      = @('SA1509=BRO1501', 'SA1510=BRO1502')
            Expected = @()
        }
        @{
            Name     = 'blank-lines-comments'
            Map      = @('SA1505=BRO1503', 'SA1515=BRO1504')
            Expected = @(
                # A comment right after a collection expression's '[' is like one after '{' (StyleCop fixed this after
                # 1.2.0-beta.556, #3766).
                'only StyleCop: BRO1504 More.cs(39,13)'
                'StyleCop output only: More.cs: []'
            )
        }
        @{
            Name     = 'element-separation'
            Map      = @('SA1516=BRO1505')
            Expected = @(
                # With documentation generated, StyleCop 1.2.0-beta.556 no longer reports a member whose '///' comment
                # directly follows the previous member (no blank line); it did with documentation off. BRO1505 reports it either way.
                'only StyleBro: BRO1505 Cases.cs(38,1)'
                # BRO1505: the blank line also goes above a '///' doc comment; StyleCop's fix leaves that one out.
                'StyleBro output only: Cases.cs: []'
                # BRO1505: two members on one line: StyleBro adds a blank line and keeps the indentation; StyleCop's
                # fix leaves a trailing space and puts the second member at column 0.
                'StyleBro output only: FileScoped.cs: [    public void M() { }]'
                'StyleBro output only: FileScoped.cs: [    public void N() { }]'
                'StyleBro output only: FileScoped.cs: []'
                'StyleCop output only: FileScoped.cs: [    public void M() { } ]'
                'StyleCop output only: FileScoped.cs: [public void N() { }]'
            )
        }
        @{
            Name     = 'comment-and-file-endings'
            Map      = @('SA1512=BRO1506', 'SA1518=BRO1507')
            Expected = @()
        }
        @{
            Name     = 'comparisons-default-values'
            Map      = @('SA1131=BRO1103', 'SA1129=BRO1104')
            Expected = @(
                # BRO1103: a comparison using a type's own operator isn't swapped (it may not be symmetric).
                'only StyleCop: BRO1103 Conditions.cs(30,17)'
                'StyleBro output only: Conditions.cs: [            b = null == m;]'
                'StyleCop output only: Conditions.cs: [            b = m == null;]'
                # BRO1104: 'new T()' on a type parameter stays ('default(T)' would skip a parameterless constructor).
                'only StyleCop: BRO1104 Creations.cs(25,21)'
                'StyleBro output only: Creations.cs: [            var g = new T();]'
                'StyleCop output only: Creations.cs: [            var g = default(T);]'
                # BRO1104: a bare 'new S();' statement stays; StyleCop's 'default(S);' doesn't compile.
                'only StyleCop: BRO1104 Creations.cs(35,13)'
                'StyleBro output only: Creations.cs: [            new Plain();]'
                'StyleCop output only: Creations.cs: [            default(Plain);]'
                'StyleCop fix doesn''t compile: Creations.cs(35,13) CS0201'
            )
        }
        @{
            Name     = 'comments-initializers'
            Map      = @('SA1005=BRO1002', 'SA1128=BRO1105')
            Expected = @(
                # BRO1002: a whitespace-only comment becomes '//'; StyleCop leaves a trailing space.
                'StyleBro output only: CommentEdges.cs: [        //]'
                'StyleCop output only: CommentEdges.cs: [        // ]'
            )
        }
        @{
            Name     = 'strings-commas'
            Map      = @('SA1122=BRO1106', 'SA1413=BRO1401')
            Expected = @(
                # BRO1401: nested multi-line initializers get both commas in one pass; StyleCop's Fix All only adds
                # the outer one, so a second 'dotnet format' run would still find the inner list.
                'StyleBro output only: CommaEdges.cs: [                    2,]'
                'StyleCop output only: CommaEdges.cs: [                    2]'
                # BRO1401: when code follows the last item directly, the fix inserts ', ' instead of ','; StyleCop's
                # '2,}' breaks SA1001 (comma followed by whitespace).
                'StyleBro output only: CommaEdges.cs: [                B = 2, };]'
                'StyleCop output only: CommaEdges.cs: [                B = 2,};]'
            )
        }
        @{
            Name     = 'parameter-layout'
            Map      = @('SA1116=BRO1107', 'SA1117=BRO1108')
            # StyleCop's SA1117 fix does nothing, so only positions are compared; StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
                # Record and primary-constructor parameters, and a primary constructor's base arguments: StyleCop
                # doesn't check these newer lists; StyleBro treats them like any other.
                'only StyleBro: BRO1107 Declarations.cs(6,25)'
                'only StyleBro: BRO1107 Declarations.cs(16,26)'
                'only StyleBro: BRO1107 Declarations.cs(17,23)'
            )
        }
        @{
            Name     = 'naming-variables-parameters'
            Map      = @('SA1312=BRO1301', 'SA1313=BRO1302')
            # StyleCop's rename fix applies only part of the renames per 'dotnet format' run (a different part each
            # time), so only positions are compared; StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
                # Names that are only underscores ('_', '__', '___') have no camelCase form; StyleBro leaves them.
                'only StyleCop: BRO1301 Names.cs(43,17)'
                'only StyleCop: BRO1302 Names.cs(40,42)'
                'only StyleCop: BRO1302 Names.cs(40,49)'
            )
        }
        @{
            Name     = 'naming-fields'
            Map      = @('SA1306=BRO1303', 'SA1309=BRO1303')
            # Output isn't compared: StyleCop's fix also renames the fields BRO1303 leaves out (below), which only
            # repeats those differences line by line. StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
                # BRO1303 checks private and protected fields. Static readonly and const fields are PascalCase
                # (SA1311/SA1303, where SA1309's underscore belongs); internal and public fields are BRO1306's.
                'only StyleCop: BRO1303 Fields.cs(16,37)'
                'only StyleCop: BRO1303 Fields.cs(18,27)'
                'only StyleCop: BRO1303 Fields.cs(24,22)'
                'only StyleCop: BRO1303 Fields.cs(26,20)'
            )
        }
        @{
            Name     = 'naming-prefixes'
            Map      = @('SA1302=BRO1304', 'SA1314=BRO1305')
            # StyleCop's fixes for SA1302/SA1314 change nothing under 'dotnet format'; positions only.
            CompareOutput = $false
            Expected = @(
            )
        }
        @{
            Name     = 'naming-pascal-fields'
            Map      = @('SA1303=BRO1306', 'SA1311=BRO1306', 'SA1307=BRO1306', 'SA1304=BRO1306')
            Expected = @(
                # StyleCop reports two rules on some fields (SA1307 with SA1311 or SA1304); BRO1306 reports them once,
                # and the comparison counts StyleCop's reports at one position once.
                # '_underscoreConst': SA1303 skips it (it doesn't start with a lower-case letter) and leaves the
                # underscore to SA1309; BRO1306 renames it to 'UnderscoreConst'.
                'only StyleBro: BRO1306 PascalFields.cs(7,27)'
                'StyleBro output only: PascalFields.cs: [        private const int UnderscoreConst = 1;]'
                'StyleBro output only: PascalFields.cs: [        public int Use() => LowerConst + PrivateLowerConst + UnderscoreConst + LowerStaticReadonly]'
                'StyleCop output only: PascalFields.cs: [        private const int _underscoreConst = 1;]'
                'StyleCop output only: PascalFields.cs: [        public int Use() => LowerConst + PrivateLowerConst + _underscoreConst + LowerStaticReadonly]'
            )
        }
        @{
            Name     = 'naming-prefix-underscore'
            Map      = @('SA1308=BRO1307', 'SA1310=BRO1308')
            # The new names differ by design, so only positions are compared: StyleCop's fix deletes the prefix or the
            # underscore and nothing else ('MAX_VALUE' -> 'MAXVALUE', 'm_Upper' -> 'Upper', 's_static' -> '@static'), and
            # needs a second run for 'm_with_more'. StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
                # 's_static' would become the keyword 'static' and 'm_' nothing: skipped (StyleCop writes '@static').
                'only StyleCop: BRO1307 Underscores.cs(8,28)'
                'only StyleCop: BRO1307 Underscores.cs(13,21)'
                # 't_thread' has [ThreadStatic]: fields with attributes aren't renamed.
                'only StyleCop: BRO1307 Underscores.cs(10,28)'
            )
        }
        @{
            Name     = 'naming-elements'
            Map      = @('SA1300=BRO1309')
            # StyleCop's SA1300 fix changes nothing under 'dotnet format'; positions only.
            CompareOutput = $false
            Expected = @(
                # Namespaces are BRO1312's (set 'naming-namespaces').
                'only StyleCop: BRO1309 Elements.cs(1,11)'
                'only StyleCop: BRO1309 Elements.cs(1,17)'
            )
        }
        @{
            Name     = 'tuple-element-casing'
            Map      = @('SA1316=BRO1311')
            # StyleCop's fix renames only the declaration (the uses then don't compile); positions only.
            CompareOutput = $false
            Expected = @(
            )
        }
        @{
            Name     = 'naming-namespaces'
            Map      = @('SA1300=BRO1312')
            Expected = @(
                # 'taken' -> 'Taken' would merge it into the existing namespace 'Taken': not reported (StyleCop merges).
                'only StyleCop: BRO1312 Taken.cs(5,11)'
                'StyleBro output only: Taken.cs: [namespace taken.Inner]'
                'StyleCop output only: Taken.cs: [namespace Taken.Inner]'
                # StyleCop's fix renames only the declaration it's on: the using directive in the other file stays behind.
                'StyleBro output only: FileScoped.cs: [using Probe.Lower;]'
                'StyleCop output only: FileScoped.cs: [using probe.lower;]'
                'StyleCop fix doesn''t compile: FileScoped.cs(3,7) CS0246'
                'StyleCop fix doesn''t compile: FileScoped.cs(7,12) CS0246'
            )
        }
        @{
            Name     = 'naming-hungarian'
            Map      = @('SA1305=BRO1310')
            # StyleCop has no fix for SA1305; positions only.
            CompareOutput = $false
            Expected = @(
                # Constant, static readonly and public fields are PascalCase (BRO1306), which removes the prefix.
                'only StyleCop: BRO1310 Hungarian.cs(8,23)'
                'only StyleCop: BRO1310 Hungarian.cs(9,33)'
                'only StyleCop: BRO1310 Hungarian.cs(13,16)'
                # Another name in the member would get the same new name (the shared rename guard): 'oValue' and the
                # field 'pValue', 'nItem' and the query's 'xItem', 'xItem' and 'yItem', 'nSize' and the method 'Size'.
                'only StyleCop: BRO1310 Hungarian.cs(21,41)'
                'only StyleCop: BRO1310 Hungarian.cs(24,22)'
                'only StyleCop: BRO1310 Hungarian.cs(44,27)'
                'only StyleCop: BRO1310 Hungarian.cs(44,47)'
                'only StyleCop: BRO1310 Hungarian.cs(54,34)'
                # '_iTotal': StyleCop's pattern doesn't look past the underscore, but BRO1303's rename to 'iTotal' would
                # make it report next time; StyleBro does both in one rename.
                'only StyleBro: BRO1310 Hungarian.cs(11,17)'
            )
        }
        @{
            Name     = 'parenthesis-placement'
            Map      = @('SA1110=BRO1109', 'SA1111=BRO1110')
            Expected = @(
                # SA1111 (1.1.118 and 1.2) doesn't check target-typed new(...) or : this(...)/: base(...) initializers;
                # BRO1110 treats them like any other argument list (found via the private app: 305 extra, 297 new()).
                'only StyleBro: BRO1110 Initializers.cs(13,13)'
                'only StyleBro: BRO1110 Initializers.cs(24,13)'
                'only StyleBro: BRO1110 Initializers.cs(31,9)'
                'StyleBro output only: Initializers.cs: [                0)]'
                'StyleBro output only: Initializers.cs: [                y)]'
                'StyleBro output only: Initializers.cs: [            0);]'
                'StyleCop output only: Initializers.cs: [                0]'
                'StyleCop output only: Initializers.cs: [            )]'
                'StyleCop output only: Initializers.cs: [                y]'
                'StyleCop output only: Initializers.cs: [            )]'
                'StyleCop output only: Initializers.cs: [            0]'
                'StyleCop output only: Initializers.cs: [        );]'
            )
        }
        @{
            Name     = 'constraints-regions'
            Map      = @('SA1127=BRO1111', 'SA1124=BRO1112', 'SA1123=BRO1113')
            Expected = @(
                # A region between switch expression arms with blank lines around it: StyleBro keeps one blank line
                # where it was (like between members), StyleCop's fix removes both.
                'StyleBro output only: Constraints.cs: []'
                'StyleBro output only: Constraints.cs: []'
            )
        }
        @{
            Name     = 'documentation-inherit'
            Map      = @('SA1600=BRO1601', 'SA1626=BRO1602')
            # StyleCop's SA1600 fix has no Fix All ('didn't return a Fix All action'), so only positions are compared.
            CompareOutput = $false
            Expected = @(
                # BRO1601 reports missing documentation only for overrides and implementations ('/// <inheritdoc/>' is
                # a real fix); a public method or an internal class without documentation would need written text.
                'only StyleCop: BRO1601 Inherit.cs(56,21)'
                'only StyleCop: BRO1601 Inherit.cs(67,20)'
            )
        }
        @{
            Name     = 'documentation'
            Map      = @('SA1629=BRO1603', 'SA1623=BRO1604', 'SA1624=BRO1605')
            Expected = @(
                # Text ending with '?', '!' or ':' is a finished sentence; StyleCop turns it into 'question?.'.
                'only StyleCop: BRO1603 Periods.cs(16,43)'
                'only StyleCop: BRO1603 Periods.cs(37,40)'
                'only StyleCop: BRO1603 Periods.cs(43,47)'
                'StyleBro output only: Periods.cs: [        /// <summary>Gets or sets ends with a question?</summary>]'
                'StyleBro output only: Periods.cs: [        /// <summary>Gets or sets ends with a colon:</summary>]'
                'StyleBro output only: Periods.cs: [        /// <summary>Gets or sets ends with an exclamation!</summary>]'
                'StyleCop output only: Periods.cs: [        /// <summary>Gets or sets ends with a question?.</summary>]'
                'StyleCop output only: Periods.cs: [        /// <summary>Gets or sets ends with a colon:.</summary>]'
                'StyleCop output only: Periods.cs: [        /// <summary>Gets or sets ends with an exclamation!.</summary>]'
                # A bool's summary may use the plain verb ('Gets the open state'); StyleCop requires 'a value indicating
                # whether' and puts it in front of any text ('whether gets the return value condition').
                'only StyleCop: BRO1604 Bools.cs(10,21)'
                'only StyleCop: BRO1604 Bools.cs(7,21)'
                'StyleBro output only: Bools.cs: [        /// <summary>Gets the return value condition.</summary>]'
                'StyleBro output only: Bools.cs: [        /// <summary>Gets or sets the flag.</summary>]'
                'StyleBro output only: Bools.cs: [        /// <summary>Gets the open state.</summary>]'
                'StyleBro output only: Bools.cs: [        /// <summary>Gets a value indicating whether it is closed.</summary>]'
                'StyleCop output only: Bools.cs: [        /// <summary>Gets a value indicating whether gets the return value condition.</summary>]'
                'StyleCop output only: Bools.cs: [        /// <summary>Gets or sets a value indicating whether gets or sets the flag.</summary>]'
                'StyleCop output only: Bools.cs: [        /// <summary>Gets a value indicating whether the open state.</summary>]'
                'StyleCop output only: Bools.cs: [        /// <summary>Gets a value indicating whether gets whether it is closed.</summary>]'
                'StyleBro output only: Properties.cs: [        /// <summary>Gets or sets is it closed.</summary>]'
                'StyleCop output only: Properties.cs: [        /// <summary>Gets or sets a value indicating whether is it closed.</summary>]'
            )
        }
        @{
            Name     = 'documentation-tags'
            Map      = @('SA1642=BRO1606', 'SA1643=BRO1607', 'SA1617=BRO1608', 'SA1651=BRO1609')
            Expected = @(
                # StyleBro puts a space after the standard sentence; StyleCop's fix doesn't ('class.Creates').
                'StyleBro output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Words"/> class. Creates a words object.</summary>]'
                'StyleBro output only: Words.cs: [        /// <summary>Initializes static members of the <see cref="Words"/> class. Initializes the static members.</summary>]'
                'StyleBro output only: Words.cs: [        /// <summary>Finalizes an instance of the <see cref="Words"/> class. Cleans up.</summary>]'
                'StyleBro output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Point"/> struct. Makes a point.</summary>]'
                'StyleBro output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Generic{T}"/> class. Makes one.</summary>]'
                'StyleBro output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Words"/> class. Initializes a new instance of Words with the given name.</summary>]'
                'StyleCop output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Words"/> class.Creates a words object.</summary>]'
                'StyleCop output only: Words.cs: [        /// <summary>Initializes static members of the <see cref="Words"/> class.Initializes the static members.</summary>]'
                'StyleCop output only: Words.cs: [        /// <summary>Finalizes an instance of the <see cref="Words"/> class.Cleans up.</summary>]'
                'StyleCop output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Point"/> struct.Makes a point.</summary>]'
                'StyleCop output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Generic{T}"/> class.Makes one.</summary>]'
                'StyleCop output only: Words.cs: [        /// <summary>Initializes a new instance of the <see cref="Words"/> class.Initializes a new instance of Words with the given name.</summary>]'
            )
        }
        @{
            Name     = 'documentation-params'
            Map      = @('SA1627=BRO1610', 'SA1612=BRO1611')
            # StyleCop has no fix for SA1627 or SA1612; positions only. StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
                # StyleCop counts a stale tag when numbering positions, so after 'old' it reports 'b' as out of order;
                # without the stale tag 'b' is where it belongs, so BRO1611 only reports 'old'.
                'only StyleCop: BRO1611 Params.cs(62,26)'
            )
        }
        @{
            Name     = 'single-line-blocks'
            Map      = @('SA1501=BRO1508', 'SA1502=BRO1509')
            # StyleCop's fix misindents nested blocks (`{` at column 1), leaves namespaces, accessors and `} catch` chains
            # partly on one line and writes CRLF into LF files; positions only. StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
                # A comment inside the braces: skipped (the fix rewrites exactly those gaps).
                'only StyleCop: BRO1508 Statements.cs(29,20)'
                # A block in a switch section that shares its line with 'switch (b) {': no line to indent it from.
                'only StyleCop: BRO1508 Statements.cs(30,37)'
                # Local functions: StyleCop reports SA1501 and SA1502 at the same brace, StyleBro one BRO1509.
                'only StyleCop: BRO1508 Statements.cs(39,26)'
                'only StyleCop: BRO1508 Statements.cs(40,31)'
            )
        }
        @{
            Name     = 'accessor-layout'
            Map      = @('SA1504=BRO1510')
            # StyleCop's fix offers 'single line' and 'multiple lines'; under Fix All one of them is applied to the whole
            # project (whichever the first diagnostic offers), and it drops comments in a body it collapses. StyleBro
            # decides per accessor list and never drops a comment. Positions only; StyleBro's output must still be clean.
            CompareOutput = $false
            Expected = @(
            )
        }
        @{
            Name     = 'documentation-typeparams'
            Map      = @('SA1613=BRO1612', 'SA1620=BRO1613', 'SA1621=BRO1614')
            CompareOutput = $false
            Expected = @(
            )
        }
        @{
            Name     = 'accessors-fields-attributes'
            Map      = @('SA1212=BRO1003', 'SA1213=BRO1004', 'SA1132=BRO1114', 'SA1125=BRO1115', 'SA1411=BRO1402')
            Expected = @(
                # SA1411: StyleCop's fix leaves the space of '( )' ('[Obsolete ]').
                'StyleBro output only: Cases.cs: [        [Obsolete]]'
                'StyleCop output only: Cases.cs: [        [Obsolete ]]'
                # SA1132: StyleCop keeps an attribute on the first field only (the second loses [Obsolete]); BRO1114 copies it.
                # Event fields are separated by the blank line BRO1505 wants.
                'StyleBro output only: Cases.cs: []'
                # SA1125: StyleCop has no fix.
                'StyleBro output only: Cases.cs: [        private int? a;]'
                'StyleBro output only: Cases.cs: [        private int? b;]'
                'StyleBro output only: Cases.cs: [        private int? c;]'
                'StyleBro output only: Cases.cs: [        private List<int?> d;]'
                'StyleBro output only: Cases.cs: [        private Type u = typeof(int?);]'
                'StyleBro output only: Cases.cs: [        public void M(bool? p)]'
                'StyleBro output only: Cases.cs: [            var x = default(long?);]'
                'StyleCop output only: Cases.cs: [        private Nullable<int> a;]'
                'StyleCop output only: Cases.cs: [        private System.Nullable<int> b;]'
                'StyleCop output only: Cases.cs: [        private global::System.Nullable<int> c;]'
                'StyleCop output only: Cases.cs: [        private List<Nullable<int>> d;]'
                'StyleCop output only: Cases.cs: [        private Type u = typeof(Nullable<int>);]'
                'StyleCop output only: Cases.cs: [        public void M(Nullable<bool> p)]'
                'StyleCop output only: Cases.cs: [            var x = default(Nullable<long>);]'
            )
        }
        @{
            Name     = 'lists-comments-docs'
            Map      = @('SA1004=BRO1005', 'SA1112=BRO1116', 'SA1113=BRO1117', 'SA1114=BRO1118', 'SA1115=BRO1119', 'SA1120=BRO1120', 'SA1136=BRO1121', 'SA1506=BRO1511', 'SA1511=BRO1512', 'SA1514=BRO1513')
            Expected = @(
                # A comment between '(' and the first item: the fix would have to move it (StyleCop has no fix).
                'only StyleCop: BRO1118 Cases.cs(54,13)'
                # Documentation right below a comment: a blank line there would break SA1512/BRO1506, and the fixes would
                # undo each other. StyleCop's fix adds it.
                'only StyleCop: BRO1513 More.cs(61,9)'
                'StyleCop output only: More.cs: []'
                # An enum on one line: StyleBro puts every value on its own line with the trailing comma (BRO1509's
                # expansion, so BRO1121 and BRO1509 agree); StyleCop's SA1136 fix leaves '{ A,' and 'C }'.
                'StyleBro output only: Cases.cs: [    public enum Values]'
                'StyleBro output only: Cases.cs: [    {]'
                'StyleBro output only: Cases.cs: [        A,]'
                'StyleBro output only: Cases.cs: [        C,]'
                'StyleBro output only: Cases.cs: [    }]'
                'StyleCop output only: Cases.cs: [    public enum Values { A,]'
                'StyleCop output only: Cases.cs: [        C }]'
                # Empty comments: StyleCop removes the reported first/last one only, leaving the next empty one to be reported
                # on the next run; StyleBro removes the whole empty run at that end of the group.
                'StyleCop output only: Cases.cs: [            //]'
                'StyleCop output only: Cases.cs: [            //    ]'
                # SA1114/SA1115 have no StyleCop fix: the blank lines in the lists stay in StyleCop's output.
                'StyleCop output only: Cases.cs: []'
                'StyleCop output only: Cases.cs: []'
                'StyleCop output only: More.cs: []'
                'StyleCop output only: Third.cs: []'
                'StyleCop output only: Third.cs: []'
                'StyleCop output only: Third.cs: []'
                'StyleCop output only: Third.cs: []'
            )
        }
        @{
            Name         = 'file-header'
            Map          = @('SA1633=BRO1615', 'SA1634=BRO1615', 'SA1635=BRO1615', 'SA1636=BRO1615', 'SA1637=BRO1615', 'SA1638=BRO1615', 'SA1640=BRO1615', 'SA1641=BRO1615')
            StyleCopJson = '{ "settings": { "documentationRules": { "companyName": "Contoso" } } }'
            EditorConfig = "stylebro_file_header_company = Contoso`n"
            Expected = @(
                # A missing header is reported on the first code, not at the start of the file, so a '#pragma warning
                # disable' at the top can suppress it. Both fixes put the header above the '#pragma'.
                'only StyleBro: BRO1615 Pragma.cs(6,1)'
                'only StyleCop: BRO1615 Pragma.cs(1,1)'
                # A header in a '/* */' comment isn't checked (rare; StyleCop rewrites it with ' *' lines).
                'only StyleCop: BRO1615 BlockWrong.cs(1,4)'
                'StyleBro output only: BlockWrong.cs: [/* <copyright file="Other.cs" company="Contoso">]'
                'StyleBro output only: BlockWrong.cs: [ Copyright (c) Contoso. All rights reserved.]'
                'StyleBro output only: BlockWrong.cs: [ </copyright> */]'
                'StyleCop output only: BlockWrong.cs: [/* <copyright file="BlockWrong.cs" company="Contoso">]'
                'StyleCop output only: BlockWrong.cs: [ * Copyright (c) Contoso. All rights reserved.]'
                'StyleCop output only: BlockWrong.cs: [ * </copyright>]'
                'StyleCop output only: BlockWrong.cs: [ */]'
                # A broken XML header (no '</copyright>') is left to a person; StyleCop replaces it.
                'only StyleCop: BRO1615 Malformed.cs(1,1)'
                'StyleCop output only: Malformed.cs: [// </copyright>]'
                # A plain comment header that isn't the copyright text: StyleCop's fix deletes it, StyleBro keeps it below
                # the new header.
                'StyleBro output only: License.cs: [// Licensed under the MIT license. See LICENSE in the repository root.]'
                'StyleBro output only: License.cs: []'
                'StyleBro output only: OnlyComment.cs: [// just a comment]'
                'StyleBro output only: PlainComment.cs: [// Some comment.]'
                'StyleBro output only: PlainComment.cs: []'
            )
        }
            @{
            Name     = 'readability-semantics'
            Map      = @('SA1139=BRO1122', 'SA1141=BRO1123', 'SA1142=BRO1124', 'SA1130=BRO1125', 'SA1135=BRO1126', 'SA1410=BRO1403')
            Expected = @(
                # Literal suffixes that change the value: (decimal) of a double rounds to 15 digits, and 1.50M keeps its scale.
                'only StyleCop: BRO1122 Literals.cs(16,28)'
                'only StyleCop: BRO1122 Literals.cs(17,28)'
                'StyleBro output only: Literals.cs: [        public decimal L = (decimal)0.1234567890123456789;]'
                'StyleBro output only: Literals.cs: [        public decimal M = (decimal)1.50;]'
                'StyleCop output only: Literals.cs: [        public decimal L = 0.1234567890123456789M;]'
                'StyleCop output only: Literals.cs: [        public decimal M = 1.50M;]'
                # Tuple types StyleCop doesn't check (arrays, nullables, locals, typeof).
                'only StyleBro: BRO1123 Tuples.cs(12,16)'
                'only StyleBro: BRO1123 Tuples.cs(14,16)'
                'only StyleBro: BRO1123 Tuples.cs(20,13)'
                'only StyleBro: BRO1123 Tuples.cs(21,31)'
                # ValueTuple.Create(x, 1) would name its first element x; new ValueTuple<int, int>() has no tuple literal
                # (StyleCop's fix writes '()').
                'only StyleCop: BRO1123 Tuples.cs(24,21)'
                'only StyleCop: BRO1123 Tuples.cs(25,21)'
                # nameof(t.Item1) would change its string.
                'only StyleCop: BRO1124 Tuples.cs(29,30)'
                # StyleCop's SA1141 fix doesn't support Fix All in Solution and its SA1142 fix throws under 'dotnet format',
                # so StyleCop leaves every tuple as it is.
                'StyleBro output only: Tuples.cs: [        public (int, string) Field;]'
                'StyleBro output only: Tuples.cs: [        public (int, (int, int)) Nested(List<(int, int)> items) => default;]'
                'StyleBro output only: Tuples.cs: [        public (int, int)[] Array;]'
                'StyleBro output only: Tuples.cs: [        public (int, int)? Maybe;]'
                'StyleBro output only: Tuples.cs: [            (long, long) local = default((long, long));]'
                'StyleBro output only: Tuples.cs: [            var type = typeof((int, int));]'
                'StyleBro output only: Tuples.cs: [            var a = (1, "a");]'
                'StyleBro output only: Tuples.cs: [            var b = (1, 2L);]'
                'StyleBro output only: Tuples.cs: [            var e = t.Count + t.Name.Length;]'
                'StyleBro output only: Tuples.cs: [            var f = maybe?.b;]'
                'StyleCop output only: Tuples.cs: [        public ValueTuple<int, string> Field;]'
                'StyleCop output only: Tuples.cs: [        public System.ValueTuple<int, ValueTuple<int, int>> Nested(List<ValueTuple<int, int>> items) => default;]'
                'StyleCop output only: Tuples.cs: [        public ValueTuple<int, int>[] Array;]'
                'StyleCop output only: Tuples.cs: [        public ValueTuple<int, int>? Maybe;]'
                'StyleCop output only: Tuples.cs: [            ValueTuple<long, long> local = default(ValueTuple<long, long>);]'
                'StyleCop output only: Tuples.cs: [            var type = typeof(ValueTuple<int, int>);]'
                'StyleCop output only: Tuples.cs: [            var a = new ValueTuple<int, string>(1, "a");]'
                'StyleCop output only: Tuples.cs: [            var b = ValueTuple.Create(1, 2L);]'
                'StyleCop output only: Tuples.cs: [            var e = t.Item1 + t.Item2.Length;]'
                'StyleCop output only: Tuples.cs: [            var f = maybe?.Item2;]'
                # StyleCop misses List<int>.ForEach(delegate (int item) ...): its overload check compares the reduced and
                # constructed method symbols. StyleBro's binds the lambda and finds the same method.
                'only StyleBro: BRO1125 Lambdas.cs(28,27)'
                'StyleBro output only: Lambdas.cs: [            items.ForEach(item => { Console.WriteLine(item); });]'
                'StyleCop output only: Lambdas.cs: [            items.ForEach(delegate (int item) { Console.WriteLine(item); });]'
                # delegate() { }: StyleCop reports SA1410 and SA1130; StyleBro only BRO1125, whose lambda replaces both.
                'only StyleCop: BRO1403 Lambdas.cs(24,32)'
                # StyleCop's lambda fix leaves two spaces after '=' and pulls a body on its own line up behind '=>'.
                'StyleBro output only: Lambdas.cs: [        private Func<int, int> twice = x => { return x * 2; };]'
                'StyleBro output only: Lambdas.cs: [            Func<int, int, int> add = (x, y) => { return x + y; };]'
                'StyleBro output only: Lambdas.cs: [            this.Changed += (s, e) =>]'
                'StyleBro output only: Lambdas.cs: [            {]'
                'StyleCop output only: Lambdas.cs: [        private Func<int, int> twice =  x => { return x * 2; };]'
                'StyleCop output only: Lambdas.cs: [            Func<int, int, int> add =  (x, y) => { return x + y; };]'
                'StyleCop output only: Lambdas.cs: [            this.Changed +=  (s, e) =>             {]'
            )
        }
            @{
            Name     = 'query-layout'
            Map      = @('SA1102=BRO1127', 'SA1103=BRO1128', 'SA1104=BRO1129', 'SA1105=BRO1130')
            # StyleCop's SA1103 fix joins the query on one line or splits it, and under 'dotnet format' which one it applies
            # changes from run to run, so the output isn't compared. StyleBro always splits.
            CompareOutput = $false
            Expected = @(
                # A mixed query that also has a multi-line clause: BRO1128 is reported with BRO1130 so one run fixes it all.
                'only StyleBro: BRO1128 Queries.cs(58,13)'
                # Blank lines with a comment line between them, and clauses sharing a line across a comment: skipped (the fix
                # would have to move the comment; StyleCop's SA1102 fix deletes it).
                'only StyleCop: BRO1127 Queries.cs(50,13)'
                'only StyleCop: BRO1128 Queries.cs(42,13)'
            )
        }
    )
}
