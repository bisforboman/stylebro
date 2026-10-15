namespace MultiTarget;

// BRO1151 in a net8.0 + net10.0 project: net10.0 binds string.Join's and Task.WhenAll's elements to .NET 9's
// 'params ReadOnlySpan' overloads, which net8.0 doesn't see, so library calls keep their arrays (trial 23). A method of
// the project's own without '#if' in its type is expanded.
public static class ParamsCalls
{
    public static string Csv(string a, string b) => string.Join(",", new[] { a, b });

    public static Task Both(Task first, Task second) => Task.WhenAll(new[] { first, second });

    public static int Total() => Numbers.Sum(new[] { 1, 2, 3 });
}

public static class Numbers
{
    public static int Sum(params int[] values) => values.Length;
}
