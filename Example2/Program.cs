using ORM.Console.Model;
using ORM.Console.Model.Entities;

namespace Example2;

public static class Program
{
    public static async Task Main()
    {
        var logEnabled = false;
        
        Patient? p1;
        {
            await using var c1 = ConsoleContext.Get(log: logEnabled);
            p1 = await Patient.Get(1);
            var p2 = await Patient.Get(2);
            var p3 = await Patient.Get(1);
            
            Console.WriteLine("P1 == P3: " + (p1 == p3));
            Console.WriteLine("P1 == P2: " + (p1 == p2));
        }

        {
            await using var c2 = ConsoleContext.Get(log: logEnabled);
            var p11 = await Patient.Get(1);

            Console.WriteLine("P1 == P11: " + (p1 == p11));
        }
    }
}