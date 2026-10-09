using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Ordering.UsingPlacementAnalyzer, StyleBro.CodeFixes.Ordering.UsingPlacementCodeFixProvider>;

namespace StyleBro.Tests;

public class UsingPlacementTests
{
    private const string Outside = "csharp_using_directive_placement = outside_namespace\n";

    [Fact]
    public Task Outside_BlockNamespace_UsingsMoveAboveIt_BelowTheHeader() => Verify.VerifyFixAsync(
        """
        // Copyright (c) Contoso.

        namespace App
        {
            {|BRO1008:using System;|}
            // Collections.
            {|BRO1008:using System.Collections.Generic;|}
            {|BRO1008:using static System.Math;|}
            {|BRO1008:using Texts = System.Collections.Generic.List<string>;|}

            public class C
            {
                public List<int> Items { get; } = new();

                public double M(Texts texts) => Abs(texts.Count) + Environment.ProcessorCount;
            }
        }
        """,
        """
        // Copyright (c) Contoso.

        using System;
        // Collections.
        using System.Collections.Generic;
        using static System.Math;
        using Texts = System.Collections.Generic.List<string>;

        namespace App
        {
            public class C
            {
                public List<int> Items { get; } = new();

                public double M(Texts texts) => Abs(texts.Count) + Environment.ProcessorCount;
            }
        }
        """,
        Outside);

    [Fact]
    public Task Outside_FileScopedNamespace_NoLeadingBlankLine() => Verify.VerifyFixAsync(
        """
        namespace App;

        {|BRO1008:using System;|}

        public class C
        {
            public Type? T { get; set; }
        }
        """,
        """
        using System;

        namespace App;

        public class C
        {
            public Type? T { get; set; }
        }
        """,
        Outside);

    [Fact]
    public Task Outside_UsingDirectlyBelowFileScopedNamespace_KeepsABlankLine() => Verify.VerifyFixAsync(
        """
        namespace App;
        {|BRO1008:using System;|}

        public class C
        {
            public Type? T { get; set; }
        }
        """,
        """
        using System;

        namespace App;

        public class C
        {
            public Type? T { get; set; }
        }
        """,
        Outside);

    [Fact]
    public Task Outside_RelativeNamesAreQualified() => Verify.VerifyFixAsync(
        """
        namespace System.Tools
        {
            {|BRO1008:using IO;|}

            public class C
            {
                public Stream? S { get; set; }
            }
        }
        """,
        """
        using System.IO;

        namespace System.Tools
        {
            public class C
            {
                public Stream? S { get; set; }
            }
        }
        """,
        Outside);

    [Fact]
    public Task Inside_IsTheDefault_UsingsMoveBelowTheBrace() => Verify.VerifyFixAsync(
        """
        // Copyright (c) Contoso.
        {|BRO1008:using System;|}
        {|BRO1008:using System.Collections.Generic;|}

        namespace App
        {
            public class C
            {
                public List<Type> Types { get; } = new();
            }
        }
        """,
        """
        // Copyright (c) Contoso.

        namespace App
        {
            using System;
            using System.Collections.Generic;

            public class C
            {
                public List<Type> Types { get; } = new();
            }
        }
        """);

    [Fact]
    public Task Inside_FileScopedNamespace() => Verify.VerifyFixAsync(
        """
        {|BRO1008:using System;|}

        namespace App;

        public class C
        {
            public Type? T { get; set; }
        }
        """,
        """
        namespace App;

        using System;

        public class C
        {
            public Type? T { get; set; }
        }
        """,
        "csharp_using_directive_placement = inside_namespace:warning\n");

    [Fact]
    public Task CorrectlyPlaced_OrPreserve_NotReported() => Task.WhenAll(
        Verify.VerifyNoDiagnosticsAsync("using System;\n\nnamespace App;\n\npublic class C { public Type? T { get; set; } }\n", Outside),
        Verify.VerifyNoDiagnosticsAsync("namespace App\n{\n    using System;\n\n    public class C { public Type? T { get; set; } }\n}\n"),
        Verify.VerifyNoDiagnosticsAsync("using System;\n\nnamespace App;\n\npublic class C { public Type? T { get; set; } }\n", "csharp_using_directive_placement = preserve\n"));

    // eShop's CS0104 and Ocelot's CS0118 under IDE0065: a name that binds to something else, or to nothing, afterwards.
    [Fact]
    public Task Outside_ANameThatWouldBindToAnEnclosingNamespacesType_NotReported() => Verify.VerifyFixAsync(
        new[]
        {
            """
            namespace App.Commands
            {
                using Domain;

                public class Handler
                {
                    public Order? Current { get; set; }
                }
            }
            """,
            "namespace Domain { public class Order { } }\nnamespace App { public class Order { } }\n",
        },
        new[]
        {
            """
            namespace App.Commands
            {
                using Domain;

                public class Handler
                {
                    public Order? Current { get; set; }
                }
            }
            """,
            "namespace Domain { public class Order { } }\nnamespace App { public class Order { } }\n",
        },
        Outside);

    // Outside, the usings join the global usings (implicit usings too): a type both import would be ambiguous (CS0104).
    [Fact]
    public Task Outside_ANameAGlobalUsingImportsToo_NotReported() => Verify.VerifyFixAsync(
        new[]
        {
            "namespace App\n{\n    using Domain;\n\n    public class Handler\n    {\n        public Order? Current { get; set; }\n    }\n}\n",
            "global using Shared;\n\nnamespace Domain { public class Order { } }\nnamespace Shared { public class Order { } }\n",
        },
        new[]
        {
            "namespace App\n{\n    using Domain;\n\n    public class Handler\n    {\n        public Order? Current { get; set; }\n    }\n}\n",
            "global using Shared;\n\nnamespace Domain { public class Order { } }\nnamespace Shared { public class Order { } }\n",
        },
        Outside);

    [Fact]
    public Task Outside_TheSameNamespaceAsAGlobalUsing_IsMoved() => Verify.VerifyFixAsync(
        new[]
        {
            "namespace App\n{\n    {|BRO1008:using Shared;|}\n\n    public class Handler\n    {\n        public Order? Current { get; set; }\n    }\n}\n",
            "global using Shared;\n\nnamespace Shared { public class Order { } }\n",
        },
        new[]
        {
            "using Shared;\n\nnamespace App\n{\n    public class Handler\n    {\n        public Order? Current { get; set; }\n    }\n}\n",
            "global using Shared;\n\nnamespace Shared { public class Order { } }\n",
        },
        Outside);

    [Fact]
    public Task Outside_ANameThatWouldBindToANamespace_NotReported() => Verify.VerifyFixAsync(
        new[]
        {
            """
            namespace Tests.DownstreamRouteFinder
            {
                using Routing.Finder;

                public class FinderTests
                {
                    public DownstreamRouteFinder? Finder { get; set; }
                }
            }
            """,
            "namespace Routing.Finder { public class DownstreamRouteFinder { } }\n",
        },
        new[]
        {
            """
            namespace Tests.DownstreamRouteFinder
            {
                using Routing.Finder;

                public class FinderTests
                {
                    public DownstreamRouteFinder? Finder { get; set; }
                }
            }
            """,
            "namespace Routing.Finder { public class DownstreamRouteFinder { } }\n",
        },
        Outside);

    [Fact]
    public Task Inside_AUsingThatWouldResolveInTheNamespace_NotReported() => Verify.VerifyFixAsync(
        new[]
        {
            """
            using Routing.Finder;

            namespace Tests.Routing
            {
                public class FinderTests
                {
                    public DownstreamRouteFinder? Finder { get; set; }
                }
            }
            """,
            "namespace Routing.Finder { public class DownstreamRouteFinder { } }\n",
        },
        new[]
        {
            """
            using Routing.Finder;

            namespace Tests.Routing
            {
                public class FinderTests
                {
                    public DownstreamRouteFinder? Finder { get; set; }
                }
            }
            """,
            "namespace Routing.Finder { public class DownstreamRouteFinder { } }\n",
        });

    [Fact]
    public Task Outside_AnExtensionMethodThatWouldChange_NotReported() => Verify.VerifyFixAsync(
        new[]
        {
            """
            namespace App.Tools
            {
                using Lib;

                public class C
                {
                    public int M() => 1.Twice();
                }
            }
            """,
            "namespace Lib { public static class E { public static int Twice(this int i) => i * 2; } }\n"
                + "namespace App { public static class E { public static int Twice(this int i) => i + i; } }\n",
        },
        new[]
        {
            """
            namespace App.Tools
            {
                using Lib;

                public class C
                {
                    public int M() => 1.Twice();
                }
            }
            """,
            "namespace Lib { public static class E { public static int Twice(this int i) => i * 2; } }\n"
                + "namespace App { public static class E { public static int Twice(this int i) => i + i; } }\n",
        },
        Outside);

    [Fact]
    public Task Skipped_Directives_SeveralNamespaces_TypesOutside_SharedLines() => Task.WhenAll(
        Verify.VerifyNoDiagnosticsAsync("#nullable enable\nusing System;\n\nnamespace App\n{\n    public class C { public Type? T { get; set; } }\n}\n"),
        Verify.VerifyNoDiagnosticsAsync("using System;\n\nnamespace App\n{\n    public class C { public Type? T { get; set; } }\n}\n\nnamespace Other\n{\n}\n"),
        Verify.VerifyNoDiagnosticsAsync("using System;\n\nnamespace App\n{\n    public class C { public Type? T { get; set; } }\n}\n\npublic class D\n{\n}\n"),
        Verify.VerifyNoDiagnosticsAsync("namespace App\n{\n#if DEBUG\n    using System;\n#endif\n\n    public class C { }\n}\n", Outside),
        Verify.VerifyNoDiagnosticsAsync("namespace App\n{\n    using System; public class C { public Type? T { get; set; } }\n}\n", Outside),
        Verify.VerifyNoDiagnosticsAsync("namespace App\n{ using System;\n\n    public class C { public Type? T { get; set; } }\n}\n", Outside),
        Verify.VerifyNoDiagnosticsAsync("namespace App; using System;\n\npublic class C { public Type? T { get; set; } }\n", Outside),
        Verify.VerifyNoDiagnosticsAsync("namespace App\n{\n    using System; public class C { }\n}\n", Outside),
        Verify.VerifyFixAsync(
            new[] { "using Routing;\n\nnamespace Tests\n{\n    public class C { }\n}\n", "namespace Routing { public class A { } }\nnamespace Tests.Routing { public class B { } }\n" },
            new[] { "using Routing;\n\nnamespace Tests\n{\n    public class C { }\n}\n", "namespace Routing { public class A { } }\nnamespace Tests.Routing { public class B { } }\n" }),
        Verify.VerifyNoDiagnosticsAsync("global using System.Text;\nusing System;\n\nnamespace App\n{\n    public class C { public Type? T { get; set; } }\n}\n"),
        Verify.VerifyNoDiagnosticsAsync("using System.Text;\n\nnamespace App\n{\n    using System;\n\n    public class C { public Type? T { get; set; } public StringBuilder? B { get; set; } }\n}\n", Outside));
}
