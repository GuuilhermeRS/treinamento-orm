using ORM.Console.Model;
using ORM.Console.Model.Entities;

namespace Example.CacheLocal;

public static class Program
{
    public static async Task Main()
    {
        // Busca por ERP_ID através de cache local
        await using var context = ConsoleContext.Get(log: true);
        var p = await Patient.GetByErpId(125);
        Console.WriteLine($"Patient: {p?.Name ?? "não"} encontrado");

        var p2 = await Patient.GetByErpId(123);
        Console.WriteLine($"Patient: {p2?.Name ?? "não"} encontrado");
                
        var p3 = await Patient.GetByErpId(120);
        Console.WriteLine($"Patient: {p3?.Name ?? "não"} encontrado");
    }
}