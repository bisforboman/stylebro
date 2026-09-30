[assembly: System.Reflection.AssemblyTrademark("probe")]
namespace FileScopedProbe;
public class C1
{
    private int a;
    public int P
    {
        get { return a; }
        set
        {
            a = value;
        }
    }
    public void M() { } public void N() { }
}
public record R(int X);
public class C2 { }
