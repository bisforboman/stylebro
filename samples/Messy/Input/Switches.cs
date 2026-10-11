namespace Messy;

/// <summary>BRO1526 'omit': BRO1519 and BRO1504 leave the gaps between switch sections alone.</summary>
public class Switches
{
    /// <summary>Classifies a value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="flag">A flag.</param>
    /// <returns>The class.</returns>
    public int Classify(object value, bool flag)
    {
        switch (value)
        {
            case int:
                return 0;
            // Strings get a block.
            case string text:
            {
                return text.Length;
            }

            case long:
                if (flag)
                {
                    return 3;
                }

                break;
            default:
                break;
        }

        return 4;
    }
}
