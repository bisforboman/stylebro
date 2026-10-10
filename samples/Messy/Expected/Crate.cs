namespace Messy;

public class Crate
{
    public Crate(string label, int weight)
    {
        Label = label;
        Weight = weight;
    }

    public string Label { get; }

    public int Weight { get; }

    public string Owner { get; set; } = string.Empty;

    // BRO1110 leaves a ')' that an initializer follows on its line: moved up, IDE0055 would put '{' below the call.
    public static Crate Pack(string label, string owner) => new Crate(
            label,
            12
        ) { Owner = owner };

    public static Crate Heavy(string label) => new Crate(
            label,
            40);
}
