namespace MultiTarget;

// net8.0 compiles this as C# 12, where 'field' is the field below; net10.0 as C# 14, where it's the property's backing
// field (warning CS9258). BRO1144's '@field' means the field in both.
public class Keywords
{
    private int field;

    public int Value
    {
        get => @field;
        set => @field = value;
    }

    public int Read() => this.field;
}
