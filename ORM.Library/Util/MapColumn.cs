namespace ORM.Core.Util;

[AttributeUsage(AttributeTargets.Property)]
public class MapColumn(string columnName) : Attribute
{
    public string ColumnName { get; } = columnName;
}