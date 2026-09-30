namespace Messy;

public interface IRepository<TEntity>
{
    TEntity Find(int id);
}

public class Store<TItem> : IRepository<TItem>
{
    public TItem Find(int id) => default!;
}