using System.Linq.Expressions;
using System.Text;
using ORM.Core.Util;

public class Restricao<T>
{
    private StringBuilder _filters = new ();
    
    private string GetPropertyColumnName<TValue>(Expression<Func<T, TValue>> propriedade)
    {
        var propertyName = ((MemberExpression)propriedade.Body).Member.Name;
        return InternalMapper.GetColumnName<T>(propertyName);
    }
    
    public void Igual<TValue>(Expression<Func<T, TValue>> propriedade, TValue valor)
    {
        var r = $"{GetPropertyColumnName(propriedade)} = '{valor}'";
        _filters.Append(r).Append(' ');
    }
    
    public void Like<TValue>(Expression<Func<T, TValue>> propriedade, string valor)
    {
        var r = $"{GetPropertyColumnName(propriedade)} LIKE '%{valor}%'";
        _filters.Append(r).Append(' ');
    }

    public void Maior<TValue>(Expression<Func<T, TValue>> propriedade, string valor)
    {
        var r =  $"{GetPropertyColumnName(propriedade)} > '{valor}'";
        _filters.Append(r).Append(' ');
    }

    public void Menor<TValue>(Expression<Func<T, TValue>> propriedade, string valor)
    {
        var r =  $"{GetPropertyColumnName(propriedade)} < '{valor}'";
        _filters.Append(r).Append(' ');
    }

    public string Open()
    {
        const string r = "(";
        _filters.Append(r).Append(' ');
        return r;
    }

    public string Close()
    {
        var r = ")";
        _filters.Append(r).Append(' ');
        return r;
    }

    public string Or()
    {
        var r = "OR";
        _filters.Append(r).Append(' ');
        return r;
    }
    
    public string And()
    {
        var r = "AND";
        _filters.Append(r).Append(' ');
        return r;
    }

    public string GetFilters()
    {
        return _filters.ToString();
    }
}

