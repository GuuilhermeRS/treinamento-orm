namespace ORM.Core;

public interface IDbSet<T>
{
    Task<T?> Find(int key);
    Task<List<T>> List();
    Task Insert(T item);
    Task Update(T item);
    Task Delete(T item);
    Task Commit();
    Task Rollback();
}