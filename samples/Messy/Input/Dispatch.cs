namespace Messy;

public class Dispatch
{
    public string Route(int code)
    {
        switch (code)
        {
            case 1:
                return "one";
            case 2:
            case 3:
                return "few";
            case 4:
            {
                var text = "four";
                return text;
            }
            default:
                return "many";
        }
    }
}
