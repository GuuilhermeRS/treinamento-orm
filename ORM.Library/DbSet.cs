
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text;
using MySqlConnector;
using ORM.Core.Util;

namespace ORM.Core;

public class DbSet<T> : IDbSet<T> where T : class, new()
{
    private readonly MySqlConnection _connection;
    private MySqlTransaction _transaction;
    private Dictionary<int, T> _cache = new();
    private readonly string _tableName = string.Empty;
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private Dictionary<string, char> _fkAlias = new();
    private static SecondLevelCache<T> _secondLevelCache = new();

    private readonly bool _logging;
    
    public DbSet(MySqlConnection connection, MySqlTransaction transaction, bool logging = false)
    {
        _connection = connection;
        _transaction = transaction;
        _logging = logging;
        if (typeof(T).GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() is TableAttribute tableNameAttribute)
        {
            _tableName = tableNameAttribute.Name;
        }
        else
        {
            //todo Add exception
        }
    }
    
    public async Task<T?> Find(int key, params Expression<Func<T, object>>[] includes)
    {
        _includes.AddRange(includes);
        var ret = await Find(key);
        _includes.Clear();
        return ret;
    }

    private string Query(bool filterPk = true)
    {
        var sb = new StringBuilder();
        const char alias = 'a';
        _fkAlias.TryAdd(typeof(T).Name, alias);
        sb
            .AppendLine($"SELECT")
            .Append('\t').AppendLine(GetColumns())
            .AppendLine("FROM")
            .Append('\t').AppendLine($"{_tableName} as {alias}");

        if (_includes.Count != 0)
        {
            sb.Append("LEFT JOIN ");

            var joinClauses = _includes.Select(include =>
            {
                if (include.Body is MemberExpression property)
                {
                    var propertyName = property.Member.Name;
                    var currentAlias = _fkAlias[propertyName]; 
                    var fk = InternalMapper.GetForeignKey<T>(propertyName);
                    var tableName = InternalMapper.GetTableName(property.Type);
                    return $" {tableName} {currentAlias} ON {alias}.{fk} = {currentAlias}.id";
                }
                return "";
            });

            sb.AppendLine(string.Join("\n\tLEFT JOIN ", joinClauses));
        }

        if (filterPk)
        {
            sb.AppendLine($"WHERE {alias}.id = @id;");
        }
        
        return sb.ToString();
    }
    
    public async Task<List<T>> Query(Restricao<T> restricoes)
    {
        var query = Query(false);
        query += $" WHERE {restricoes.GetFilters()};";

        await using var command = new MySqlCommand(query, _connection, _transaction);
        await using var reader = await command.ExecuteReaderAsync();
        if (_logging)
            Console.WriteLine($"[{DateTime.Now}]\n{query}");

        var result = new List<T>();
        while (reader.Read())
        {
            var r = InternalMapper.MapTo<T>(reader, _fkAlias);
            result.Add(r);
        }
        
        foreach (var item in result)
        {
            var key = (int) InternalMapper.GetKey<T>().GetValue(item)!;
            _cache.TryAdd(key, item);
            _secondLevelCache.Add(key, item);
        }

        return result;
    }
    
    
    public async Task<T?> Find(int key)
    {
        if (_cache.TryGetValue(key, out var result))
            return result;

        if (_secondLevelCache.TryGetValue(key, out result))
            return result;
        
        var query = Query();

        await using var command = new MySqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("@id", key);
        await using var reader = await command.ExecuteReaderAsync();
        if (_logging)
            Console.WriteLine($"[{DateTime.Now}]\n{query} - @id = {key}");
        
        if (reader.Read())
        {
            result = InternalMapper.MapTo<T>(reader, _fkAlias);
            _cache.Add(key, result);
            _secondLevelCache.Add(key, result);
        }

        return result;
    }

    public async Task<List<T>> List()
    {
        var columns = string.Join(", ", InternalMapper.GetColumns<T>());
        var query = $"SELECT {columns} FROM {_tableName};";

        await using var command = new MySqlCommand(query, _connection, _transaction);
        await using var reader = await command.ExecuteReaderAsync();
        if (_logging)
            Console.WriteLine($"[{DateTime.Now}]\n{query}");
        var ret = new List<T>();
        
        while (reader.Read())
        {
            ret.Add(InternalMapper.MapTo<T>(reader));
        }
        
        foreach (var item in ret)
        {
            var key = (int) InternalMapper.GetKey<T>().GetValue(item)!;
            _cache.Add(key, item);
            _secondLevelCache.Add(key, item);
        }
        
        return ret;
    }

    public async Task Insert(T item)
    {
        var columns = InternalMapper.GetColumnsWithValues(item);
        if (!columns.Any())
            return;

        var values = columns.Values.Select(s => $"@value{columns.Values.ToList().IndexOf(s)}").ToList();
        var query = $"INSERT INTO {_tableName} ({string.Join(", ", columns.Keys)}) VALUES ({string.Join(", ", values)}); SELECT LAST_INSERT_ID();";
        
        await using var command = new MySqlCommand(query, _connection, _transaction);
        values.ForEach(s => command.Parameters.AddWithValue(s, columns.Values.ElementAt(values.IndexOf(s))));
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (_logging)
            Console.WriteLine($"[{DateTime.Now}]\n{query}");

        var key = typeof(T).GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
        key?.SetValue(item, id);
        
        _cache.Add(id, item);
        _secondLevelCache.Add(id, item);
    }

    public async Task Update(T item)
    {
        var key = (int) InternalMapper.GetKey<T>().GetValue(item)!;
        var columns = InternalMapper.GetColumnsWithValues(item);
        if (!columns.Any())
            return;
        
        var values = columns.Values.Select(s => $"@value{columns.Values.ToList().IndexOf(s)}").ToList();
        var query = $"UPDATE {_tableName} SET {string.Join(", ", columns.Keys.Select(s => $"{s} = {values[columns.Keys.ToList().IndexOf(s)]}"))} WHERE id = {key};";
        
        await using var command = new MySqlCommand(query, _connection, _transaction);
        if (_logging)
            Console.WriteLine($"[{DateTime.Now}]\n{query}");
        
        values.ForEach(s => command.Parameters.AddWithValue(s, columns.Values.ElementAt(values.IndexOf(s))));
        await command.ExecuteScalarAsync();
    }

    public async Task Delete(T item)
    {
        var key = (int) InternalMapper.GetKey<T>().GetValue(item)!;
        var query = $"DELETE FROM {_tableName} WHERE id = @id;";
        
        await using var command = new MySqlCommand(query, _connection, _transaction);
        command.Parameters.AddWithValue("@id", key);
        await command.ExecuteNonQueryAsync();
        if (_logging)
            Console.WriteLine($"[{DateTime.Now}]\n{query} - @id = {key}");

        _cache.Remove(key);
    }

    public Task Commit()
    {
        return Task.CompletedTask;
    }

    public Task Rollback()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public void UpdateTransaction(MySqlTransaction transaction)
    {
        _transaction = transaction;
    }
    
    private string GetColumns()
    {
        var alias = 'a';
        var columns = InternalMapper.GetColumns<T>().Select(s => $"{alias}.{s} as {alias}_{s}").ToList();

        if (_includes.Count == 0)
            return string.Join(", ", columns);
        
        foreach (var include in _includes)
        {
            var internalAlias = ++alias;
            if (include.Body is MemberExpression property)
            {
                _fkAlias.Add(property.Member.Name, internalAlias);
                var c = InternalMapper.GetColumns(property.Type);
                columns.AddRange(c.Select(s => $"{internalAlias}.{s} as {internalAlias}_{s}"));
            }
        }

        return string.Join(", ", columns);
    }
}