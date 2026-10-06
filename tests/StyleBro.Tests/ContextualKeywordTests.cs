using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ContextualKeywordAnalyzer, StyleBro.CodeFixes.Readability.ContextualKeywordCodeFixProvider>;

namespace StyleBro.Tests;

// Roslyn 4.8 parses C# 12, so these cover the code as written before C# 14 (where 'field', 'extension' and 'partial'
// are plain identifiers). The C# 14 parse ('field' keyword) is checked end to end in samples/CSharp14.
public class ContextualKeywordTests
{
    [Fact]
    public Task Field_InPropertyAccessors_IsEscaped() => Verify.VerifyFixAsync(
        """
        using System;
        using System.Linq;

        public class C
        {
            private int field;

            public int A
            {
                get => {|BRO1144:field|};
                set => {|BRO1144:field|} = value;
            }

            public int B => {|BRO1144:field|} + 1;

            public int D
            {
                get
                {
                    Func<int> f = () => {|BRO1144:field|};
                    int L() => {|BRO1144:field|};
                    return f() + L() + nameof({|BRO1144:field|}).Length;
                }
            }

            public int E
            {
                get
                {
                    var {|BRO1144:field|} = 1;
                    return {|BRO1144:field|};
                }
            }

            public Func<int, int> E2 => {|BRO1144:field|} => 2;

            public int E3 => (from {|BRO1144:field|} in new[] { 1 } select {|BRO1144:field|}).Count();

            public int F
            {
                get => 0;
                set
                {
                    if (value is int {|BRO1144:field|})
                    {
                        this.field = {|BRO1144:field|};
                    }
                }
            }
        }
        """,
        """
        using System;
        using System.Linq;

        public class C
        {
            private int field;

            public int A
            {
                get => @field;
                set => @field = value;
            }

            public int B => @field + 1;

            public int D
            {
                get
                {
                    Func<int> f = () => @field;
                    int L() => @field;
                    return f() + L() + nameof(@field).Length;
                }
            }

            public int E
            {
                get
                {
                    var @field = 1;
                    return @field;
                }
            }

            public Func<int, int> E2 => @field => 2;

            public int E3 => (from @field in new[] { 1 } select @field).Count();

            public int F
            {
                get => 0;
                set
                {
                    if (value is int @field)
                    {
                        this.field = @field;
                    }
                }
            }
        }
        """);

    [Fact]
    public Task Field_WhereItStaysAName_IsNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private int field;
            private int other;

            public C()
            {
                field = 1;
            }

            public int A
            {
                get => this.field + @field + new C().field + this?.field ?? 0;
            }

            public int B
            {
                get
                {
                    var a = new { field = 1 };
                    return M(field: a.field);
                }
            }

            public int D { get; set; } = field2;

            public int this[int i] => field;

            public event System.Action E
            {
                add { field++; }
                remove { }
            }

            private static int field2 = 0;

            public int M() => field;

            public static int M(int field) => field;
        }
        """);

    [Fact]
    public Task Extension_AsATypeName_IsEscaped() => Verify.VerifyFixAsync(
        """
        using {|BRO1144:extension|} = N.@extension;

        namespace N
        {
            public class @extension
            {
            }

            public class {|BRO1144:extension|}<T>
            {
                public {|BRO1144:extension|}(int x)
                {
                }

                public static {|BRO1144:extension|}<T> Create() => new extension<T>(0);

                [System.Obsolete]
                {|BRO1144:extension|}<T> Self => this;

                {|BRO1144:extension|}<T>[] all;

                private void M(extension<T> e)
                {
                    extension<T> local = e;
                }
            }

            public class G<{|BRO1144:extension|}>
            {
            }
        }

        namespace S
        {
            public struct {|BRO1144:extension|}
            {
            }
        }

        namespace D
        {
            public delegate void {|BRO1144:extension|}(int x);
        }
        """,
        """
        using @extension = N.@extension;

        namespace N
        {
            public class @extension
            {
            }

            public class @extension<T>
            {
                public @extension(int x)
                {
                }

                public static @extension<T> Create() => new extension<T>(0);

                [System.Obsolete]
                @extension<T> Self => this;

                @extension<T>[] all;

                private void M(extension<T> e)
                {
                    extension<T> local = e;
                }
            }

            public class G<@extension>
            {
            }
        }

        namespace S
        {
            public struct @extension
            {
            }
        }

        namespace D
        {
            public delegate void @extension(int x);
        }
        """);

    [Fact]
    public Task Partial_AsAReturnType_IsEscaped() => Verify.VerifyFixAsync(
        """
        public class partial
        {
        }

        public partial class C
        {
            private {|BRO1144:partial|} F() => new partial();

            private partial[] G() => new partial[0];

            private partial P => new partial();

            private void M(partial p)
            {
                {|BRO1144:partial|} L() => p;
                partial x = L();
            }

            partial void N();
        }
        """,
        """
        public class partial
        {
        }

        public partial class C
        {
            private @partial F() => new partial();

            private partial[] G() => new partial[0];

            private partial P => new partial();

            private void M(partial p)
            {
                @partial L() => p;
                partial x = L();
            }

            partial void N();
        }
        """);
}
