using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.RedundantJumpAnalyzer, StyleBro.CodeFixes.Readability.RedundantJumpCodeFixProvider>;

namespace StyleBro.Tests;

public class RedundantJumpTests
{
    [Fact]
    public Task ReturnAtTheEndOfABody_IsRemoved() => Verify.VerifyFixAsync(
        """
        using System;
        using System.Threading.Tasks;

        public class C
        {
            private int value;

            public C()
            {
                this.value = 1;
                {|BRO1137:return;|}
            }

            public int Value
            {
                get => this.value;
                set
                {
                    this.value = value;

                    {|BRO1137:return;|}
                }
            }

            public void M() { {|BRO1137:return;|} }

            public async Task N()
            {
                await Task.Yield(); {|BRO1137:return;|}
            }

            public void O()
            {
                Action a = () => { this.M(); {|BRO1137:return;|} };
                void Local()
                {
                    a();
                    {|BRO1137:return;|}
                }

                Local();
            }
        }
        """,
        """
        using System;
        using System.Threading.Tasks;

        public class C
        {
            private int value;

            public C()
            {
                this.value = 1;
            }

            public int Value
            {
                get => this.value;
                set
                {
                    this.value = value;
                }
            }

            public void M() { }

            public async Task N()
            {
                await Task.Yield();
            }

            public void O()
            {
                Action a = () => { this.M(); };
                void Local()
                {
                    a();
                }

                Local();
            }
        }
        """);

    [Fact]
    public Task YieldBreakAtTheEndOfAnIterator_IsRemoved() => Verify.VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public IEnumerable<int> M(bool b)
            {
                if (b)
                {
                    yield return 1;
                }

                {|BRO1137:yield break;|}
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C
        {
            public IEnumerable<int> M(bool b)
            {
                if (b)
                {
                    yield return 1;
                }
            }
        }
        """);

    // akka.net: without its 'return;' the lambda's end is unreachable, so it also converts to Predicate<T> and the call is
    // ambiguous (CS0121). With a reachable end or another 'return;', the statement goes.
    [Fact]
    public Task LambdaWhoseEndWouldBeUnreachable_KeepsItsReturn() => Verify.VerifyFixAsync(
        """
        using System;

        public class C
        {
            public void Receive<T>(Action<T> handler) { }

            public void Receive<T>(Predicate<T> handler) { }

            public void Setup(bool b)
            {
                Receive<string>(_ => { throw new InvalidOperationException(); return; });
                Receive<string>(_ =>
                {
                    if (b)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        throw new ArgumentException();
                    }

                    return;
                });
                Receive<string>(_ => { Console.WriteLine(); {|BRO1137:return;|} });
                Receive<string>(_ => { {|BRO1137:return;|} });
                Receive<string>(_ =>
                {
                    if (b)
                    {
                        return;
                    }

                    while (true)
                    {
                    }

                    {|BRO1137:return;|}
                });
            }
        }
        """,
        """
        using System;

        public class C
        {
            public void Receive<T>(Action<T> handler) { }

            public void Receive<T>(Predicate<T> handler) { }

            public void Setup(bool b)
            {
                Receive<string>(_ => { throw new InvalidOperationException(); return; });
                Receive<string>(_ =>
                {
                    if (b)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        throw new ArgumentException();
                    }

                    return;
                });
                Receive<string>(_ => { Console.WriteLine(); });
                Receive<string>(_ => { });
                Receive<string>(_ =>
                {
                    if (b)
                    {
                        return;
                    }

                    while (true)
                    {
                    }
                });
            }
        }
        """);

    [Fact]
    public Task StatementsThatMatter_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Collections.Generic;

        public class C
        {
            public IEnumerable<int> OnlyYield()
            {
                static IEnumerable<int> Inner()
                {
                    yield return 1;
                }

                Func<IEnumerable<int>> f = () => Inner();
                yield break;
            }

            public int Value() { return 1; }

            public void Nested(bool b)
            {
                if (b)
                {
                    Console.WriteLine();
                    return;
                }

                Console.WriteLine(b);
            }

            public void Commented()
            {
                Console.WriteLine();
                return; // done
            }

            public void CommentedAbove()
            {
                Console.WriteLine();

                // done
                return;
            }

            public void NotLast()
            {
                return;
                Console.WriteLine();
            }

            public void Labeled()
            {
                goto end;
            end:
                return;
            }

            public void Directive()
            {
                Console.WriteLine();
        #if !UNDEFINED
                return;
        #endif
            }
        }
        """);
}
