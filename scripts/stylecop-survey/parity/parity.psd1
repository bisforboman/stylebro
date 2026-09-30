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
    )
}
