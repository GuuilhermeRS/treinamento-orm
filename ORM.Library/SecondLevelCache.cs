using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ORM.Core;

public class SecondLevelCache<T> where T : class
{
    private static Dictionary<int, T> _cache = new ();
    
    public void Add(int key, T value)
    {
        if (_cache.ContainsKey(key))
            return;
        
        var v = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value))!;
        _cache.Add(key, v);
    }
    
    public T? Get(int key)
    {
        var v = _cache.GetValueOrDefault(key);

        return v is not null
            ? JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v))
            : null;
    }

    public bool TryGetValue(int key, out T? value)
    {
        value = null;
        if (_cache.TryGetValue(key, out var v))
        {
            value = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v))!;
            return true;
        }

        return false;
    }
    
    public static void Remove(int key)
    {
        _cache.Remove(key);
    }
    
    public static void Clear()
    {
        _cache.Clear();
    }
}