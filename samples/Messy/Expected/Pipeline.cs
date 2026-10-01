namespace Messy;

public class Stage
{
    public Stage(string name) => Name = name;

    public string Name { get; }
}

//------------------------------------------------------------
// A stage with a default name.
public class Pipeline : Stage
{
    public Pipeline()
        : this("default")
    {
    }

    public Pipeline(string name)
        : base(name)
    {
        // TODO: validate the name
        ////Console.WriteLine(name);
    }
}