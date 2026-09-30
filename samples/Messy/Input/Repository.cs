namespace Messy;

public interface Repository<Entity>
{
    Entity Find(int id);
}

public class Store<Item> : Repository<Item>
{
    public Item Find(int id) => default!;
}