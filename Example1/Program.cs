using ORM.Console.Model;
using ORM.Console.Model.Entities;

namespace Example1;

public static class Program
{
    public static async Task Main()
    {
        await using var con = ConsoleContext.Get(true);

        var x = new Restricao<Patient>();
        x.Open();
        x.Igual(s => s.Id, 23);
        x.Close();
        x.Or();
        x.Like(s => s.Name, "Guilherme");

        var a = await Patient.Query(x);

        Console.WriteLine(string.Join(", ", a.Select(s => s.Name)));
    }
}

