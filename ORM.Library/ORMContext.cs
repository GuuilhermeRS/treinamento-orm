using System.Reflection;
using MySqlConnector;

namespace ORM.Core;

public class OrmContext : IAsyncDisposable, IDisposable
{
    private readonly MySqlConnection _connection;
    private MySqlTransaction? _transaction;
    private readonly bool _logging;

    protected OrmContext(string connectionString, bool startTransaction = false, bool logging = false)
    {
        _connection = new MySqlConnection(connectionString);
        _connection.Open();
        _logging = logging;

        if (startTransaction)
        {
            _transaction = _connection.BeginTransaction();
        }

        ConfigureDataSet();
    }
    
    private void ConfigureDataSet()
    {
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                var entityType = field.FieldType.GetGenericArguments()[0];
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
                var dbSet = Activator.CreateInstance(dbSetType, _connection, _transaction, _logging);
                field.SetValue(this, dbSet);
            }
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                var entityType = field.FieldType.GetGenericArguments()[0];
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
                var instance = field.GetValue(this);
                var method = dbSetType.GetMethod("Rollback");
                method!.Invoke(instance, null);
            }
        }
    }

    public async Task SaveChangesAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            _transaction = await _connection.BeginTransactionAsync();
        }
        
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                var entityType = field.FieldType.GetGenericArguments()[0];
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
                var instance = field.GetValue(this);
                var method = dbSetType.GetMethod("Commit");
                method!.Invoke(instance, null);
                
                method = dbSetType.GetMethod("UpdateTransaction");
                method!.Invoke(instance, [_transaction]);
            }
        }
    }

    public virtual void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}