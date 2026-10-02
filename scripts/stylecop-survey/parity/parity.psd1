# Sets for Compare-WithStyleCop.ps1. Each folder holds case files; Map pairs each StyleCop rule with its StyleBro
# rule. Expected lists every difference the comparison is allowed to find: StyleBro's documented deviations
# (see docs/rules). Output lines are shown untrimmed, in brackets.
@{
    Sets = @(
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
            Expected = @()
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
                # BRO1303 checks private fields only. Static readonly and const fields are PascalCase (SA1311/SA1303,
                # where SA1309's underscore belongs); protected, internal and public fields are visible outside the type.
                'only StyleCop: BRO1303 Fields.cs(16,37)'
                'only StyleCop: BRO1303 Fields.cs(18,27)'
                'only StyleCop: BRO1303 Fields.cs(19,23)'
                'only StyleCop: BRO1303 Fields.cs(21,23)'
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
                # Namespaces aren't renamed: that also changes embedded resource names and breaks folder conventions.
                'only StyleCop: BRO1309 Elements.cs(1,11)'
                'only StyleCop: BRO1309 Elements.cs(1,17)'
            )
        }
        @{
            Name     = 'parenthesis-placement'
            Map      = @('SA1110=BRO1109', 'SA1111=BRO1110')
            Expected = @(
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
    )
}
