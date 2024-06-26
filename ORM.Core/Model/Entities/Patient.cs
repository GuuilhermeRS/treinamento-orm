using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ORM.Core.Util;

namespace ORM.Console.Model.Entities;

[Table("patient")]
public class Patient
{
    [Key, MapColumn("id")] public int Id { get; set; }
    [MapColumn("name")] public string Name { get; set; }
    [MapColumn("erp_id")] public int ErpId { get; set; }

    public Patient()
    {
    }

    public Patient(string name)
    {
        Name = name;
        ConsoleContext.Get().PatientSet.Insert(this).GetAwaiter().GetResult();
    }

    public static async Task<List<Patient>> List()
    {
        return await ConsoleContext.Get().PatientSet.List();
    }

    public static async Task<Patient?> Get(int id)
    {
        return await ConsoleContext.Get()
            .PatientSet.Find(id);
    }

    public static async Task<List<Patient>> Query(Restricao<Patient> restricao)
    {
        return await ConsoleContext.Get().PatientSet
            .Query(restricao);
    }

    public static async Task<Patient?> GetByErpId(int erpId)
    {
        return await ConsoleContext.Get().GetPatientByErpId(erpId);
    }
}