using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.PascalCaseNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class AsyncSuffixTests
{
    private const string On = "dotnet_diagnostic.BRO1314.severity = warning";

    [Fact]
    public Task AwaitableMethods_GetTheSuffix_WithEveryCall() => VerifyFixAsync(
        """
        using System.Collections.Generic;
        using System.Threading.Tasks;

        public class Store
        {
            public Task {|BRO1314:Save|}() => Task.CompletedTask;

            public Task<int> {|BRO1314:Count|}() => Task.FromResult(1);

            public ValueTask {|BRO1314:Flush|}() => default;

            public ValueTask<int> {|BRO1314:Peek|}() => default;

            public async IAsyncEnumerable<int> {|BRO1314:Items|}()
            {
                await Task.Yield();
                yield return 1;
            }

            public Task LoadAsync() => Task.CompletedTask;

            public int Total() => 0;

            public async void Fire() => await Save();

            [System.Obsolete]
            public async Task<int> {|BRO1314:Run|}()
            {
                await Save();
                await Flush();
                await Peek();
                await foreach (var item in Items())
                {
                }

                return await Count();
            }
        }
        """,
        """
        using System.Collections.Generic;
        using System.Threading.Tasks;

        public class Store
        {
            public Task SaveAsync() => Task.CompletedTask;

            public Task<int> CountAsync() => Task.FromResult(1);

            public ValueTask FlushAsync() => default;

            public ValueTask<int> PeekAsync() => default;

            public async IAsyncEnumerable<int> ItemsAsync()
            {
                await Task.Yield();
                yield return 1;
            }

            public Task LoadAsync() => Task.CompletedTask;

            public int Total() => 0;

            public async void Fire() => await SaveAsync();

            [System.Obsolete]
            public async Task<int> RunAsync()
            {
                await SaveAsync();
                await FlushAsync();
                await PeekAsync();
                await foreach (var item in ItemsAsync())
                {
                }

                return await CountAsync();
            }
        }
        """,
        On);

    [Fact]
    public Task Casing_AndSuffix_InOneRename() => VerifyFixAsync(
        """
        using System.Threading.Tasks;

        public class C
        {
            public Task {|BRO1314:load|}() => Task.CompletedTask;

            public Task {|BRO1314:Use|}() => load();
        }
        """,
        """
        using System.Threading.Tasks;

        public class C
        {
            public Task LoadAsync() => Task.CompletedTask;

            public Task UseAsync() => LoadAsync();
        }
        """,
        On);

    [Fact]
    public Task CasingOff_OnlyTheSuffix() => VerifyFixAsync(
        """
        using System.Threading.Tasks;

        public class C
        {
            public Task {|BRO1314:save|}() => Task.CompletedTask;
        }
        """,
        """
        using System.Threading.Tasks;

        public class C
        {
            public Task saveAsync() => Task.CompletedTask;
        }
        """,
        On + "\ndotnet_diagnostic.BRO1309.severity = none");

    [Fact]
    public Task BaseMembers_AreRenamedWithOverridesAndImplementations() => VerifyFixAsync(
        """
        using System.Threading.Tasks;

        public interface IStore
        {
            Task {|BRO1314:Save|}(int id);
        }

        public class Store : IStore
        {
            public Task Save(int id) => Task.CompletedTask;
        }

        public class Explicit : IStore
        {
            Task IStore.Save(int id) => Task.CompletedTask;
        }

        public abstract class Base
        {
            public abstract Task {|BRO1314:Write|}();
        }

        public class Derived : Base
        {
            public override Task Write() => Task.CompletedTask;
        }
        """,
        """
        using System.Threading.Tasks;

        public interface IStore
        {
            Task SaveAsync(int id);
        }

        public class Store : IStore
        {
            public Task SaveAsync(int id) => Task.CompletedTask;
        }

        public class Explicit : IStore
        {
            Task IStore.SaveAsync(int id) => Task.CompletedTask;
        }

        public abstract class Base
        {
            public abstract Task WriteAsync();
        }

        public class Derived : Base
        {
            public override Task WriteAsync() => Task.CompletedTask;
        }
        """,
        On);

    [Fact]
    public Task OffByDefault() => VerifyNoDiagnosticsAsync(
        """
        using System.Threading.Tasks;

        public class C
        {
            public Task Save() => Task.CompletedTask;
        }
        """);

    [Fact]
    public Task SkippedMethods_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Threading.Tasks;

        [AttributeUsage(AttributeTargets.All)]
        public sealed class FactAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.All)]
        public sealed class ApiControllerAttribute : Attribute
        {
        }

        public class Program
        {
            public static Task Main() => Task.CompletedTask;

            public Task ReadAsyncCore() => Task.CompletedTask;

            [Fact]
            public Task Saves() => Task.CompletedTask;

            public Task OnClick(object sender, EventArgs e) => Task.CompletedTask;

            public Task Other() => Task.CompletedTask;

            public Task OtherAsync() => Task.CompletedTask;

            public Task OuterAsync()
            {
                return Inner();

                Task Inner() => Task.CompletedTask;
            }
        }

        public class ControllerBase
        {
        }

        public class Orders : ControllerBase
        {
            public Task Get() => Task.CompletedTask;
        }

        public class OrdersController
        {
            public Task Get() => Task.CompletedTask;
        }

        [ApiController]
        public class Api
        {
            public Task Get() => Task.CompletedTask;
        }

        public class Disposer : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => default;
        }

        public class Work : IProgress<int>
        {
            public void Report(int value)
            {
            }
        }
        """,
        On);

    [Fact]
    public Task OtherTypesNamedTask_AreNotAwaitable() => VerifyNoDiagnosticsAsync(
        """
        namespace Jobs
        {
            public class Task
            {
            }

            public class ValueTask<T>
            {
            }

            public class Factory
            {
                public Task Make() => new Task();

                public ValueTask<int> Wrap() => new ValueTask<int>();
            }
        }
        """,
        On);

    [Fact]
    public Task ImplementationOfAnotherInterface_KeepsTheName() => VerifyNotFixedAsync(
        new[]
        {
            """
            using System.Threading.Tasks;

            public interface IRunner
            {
                Task {|BRO1314:Run|}();
            }

            public interface IJob
            {
                Task {|BRO1314:Run|}();
            }

            public class Job : IRunner, IJob
            {
                public Task Run() => Task.CompletedTask;
            }
            """,
        },
        On + "\ndotnet_diagnostic.BRO1309.severity = warning");

    [Fact]
    public Task NameInAString_KeepsTheName() => VerifyNotFixedAsync(
        new[]
        {
            """
            using System.Threading.Tasks;

            public class C
            {
                public Task {|BRO1314:Save|}() => Task.CompletedTask;

                public object Find() => typeof(C).GetMethod("Save");
            }
            """,
        },
        On);
}
