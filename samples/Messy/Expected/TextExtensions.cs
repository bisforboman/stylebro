namespace Messy;

// C# 14 (this sample targets net10.0): extension blocks (BRO1001 sorts them right before the methods and sorts their
// members; BRO1509 expands a one-line block) and the 'field' keyword next to a field named 'field' (BRO1144).
public static class TextExtensions
{
    private const int Limit = 10;

    extension(long v)
    {
        public long Half => v / 2;
    }

    extension(string s)
    {
        public int Size => s.Hidden;

        private int Hidden => s.Length;

        public bool IsShort() => s.Length < Limit;
    }

    public static int Twice(int x) => x * 2;
}

// A primary constructor parameter named 'field' (a private field named 'field' would be kept by BRO1303's string guard
// here: the StyleBro projects this sample references contain the string "field").
public class BackingStore(int field)
{
    public int Reading
    {
        get => @field * 2;
    }

    // A string property: here the keyword can only mean its own backing field, so it stays.
    public string Label
    {
        get => field;
        set => field = value.Trim();
    } = string.Empty;
}
