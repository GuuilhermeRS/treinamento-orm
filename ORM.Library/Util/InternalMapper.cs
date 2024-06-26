using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace ORM.Core.Util;
public static class InternalMapper
{
    public static T MapTo<T>(IDataReader reader)  where T : new()
    {
        var item = new T();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            var attribute = (MapColumn)property.GetCustomAttribute(typeof(MapColumn))!;
            if (attribute != null)
            {
                var columnNameWithAlias = $"a.{attribute.ColumnName}";
                var value = reader[reader.GetOrdinal(attribute.ColumnName)];
                property.SetValue(item, value);
            }
        }

        return item;
    }
    
    public static T MapTo<T>(IDataReader reader, Dictionary<string, char> fkAliases) where T : new()
    {
        var item = new T();
        var properties = typeof(T).GetProperties();
        var alias = fkAliases[typeof(T).Name];

        foreach (var property in properties)
        {
            var attribute = (MapColumn)property.GetCustomAttribute(typeof(MapColumn))!;
            if (attribute != null)
            {
                var columnName = $"{alias}_{attribute.ColumnName}";
                var value = reader[reader.GetOrdinal(columnName)];
                property.SetValue(item, value);
            }
            else if (fkAliases.TryGetValue(property.Name, out var internalAlias))
            {
                var fkObject = MapTo(property.PropertyType, reader, internalAlias);
                property.SetValue(item, fkObject);
            }
        }

        return item;
    }
    
    public static object MapTo(Type entityType, IDataReader reader, char alias = 'a')
    {
        var item = Activator.CreateInstance(entityType);
        var properties = entityType.GetProperties();

        foreach (var property in properties)
        {
            var attribute = (MapColumn)property.GetCustomAttribute(typeof(MapColumn))!;
            if (attribute != null)
            {
                var columnNameWithAlias = $"{alias}_{attribute.ColumnName}";
                var value = reader[reader.GetOrdinal(columnNameWithAlias)];
                property.SetValue(item, value);
            }
        }

        return item;
    }
    
    public static IEnumerable<string> GetColumns<T>()
    {
        return typeof(T)
            .GetProperties()
            .Where(p => p.GetCustomAttribute<MapColumn>() != null)
            .Select(p => p.GetCustomAttribute<MapColumn>()!.ColumnName);
    }
    
    public static Dictionary<string, object?> GetColumnsWithValues<T>(T obj)
    {
        return typeof(T).GetProperties()
            .Where(s => !Attribute.IsDefined(s, typeof(KeyAttribute)))
            .ToDictionary(p => p.GetCustomAttribute<MapColumn>()!.ColumnName, p => p.GetValue(obj));
    }

    public static PropertyInfo GetKey<T>()
    {
        return typeof(T).GetProperties()
            .First(p => p.GetCustomAttributes<KeyAttribute>().Any());
    }

    public static string GetTableName(Type entityType)
    {
        if (entityType.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() is TableAttribute tableAttribute)
        {
            return tableAttribute.Name;
        }
            
        throw new InvalidOperationException($"TableAttribute not found for type {entityType.Name}");
    }
    
    public static string GetForeignKey<T>(string propertyFkName)
    {
        var properties = typeof(T).GetProperties();
        var fkProperty = properties.FirstOrDefault(p =>
        {
            var fkAttribute = p.GetCustomAttributes(typeof(ForeignKeyAttribute), true).FirstOrDefault() as ForeignKeyAttribute;
            return fkAttribute != null && fkAttribute.Name == propertyFkName;
        });
        
        if (fkProperty is null)
            throw new InvalidOperationException($"Foreign Key {propertyFkName} not found for type {typeof(T).Name}");

        var columnAttribute = fkProperty.GetCustomAttributes(typeof(MapColumn), true).FirstOrDefault() as MapColumn;
        
        return columnAttribute!.ColumnName;
    }
    
    public static string GetColumnName<T>(string propertyName)
    {
        var properties = typeof(T).GetProperties();
        var property = properties.FirstOrDefault(p => p.Name == propertyName);
        
        if (property is null)
            throw new InvalidOperationException($"Foreign Key {propertyName} not found for type {typeof(T).Name}");

        var columnAttribute = property.GetCustomAttributes(typeof(MapColumn), true).FirstOrDefault() as MapColumn;
        
        return columnAttribute!.ColumnName;
    }
    
    public static List<string> GetColumns(Type entityType)
    {
        var columns = new List<string>();
        var properties = entityType.GetProperties();

        foreach (var property in properties)
        {
            var mapColumnAttribute = property.GetCustomAttributes(typeof(MapColumn), true).FirstOrDefault() as MapColumn;
            var columnName = mapColumnAttribute?.ColumnName ?? property.Name;
            columns.Add(columnName);
        }

        return columns;
    }
}