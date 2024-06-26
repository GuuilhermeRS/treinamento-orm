using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ORM.Core.Util;

namespace ORM.Console.Model.Entities;

[Table("attendance")]
public class Attendance
{
    [Key, MapColumn("id")] public int Id { get; set; }
    [MapColumn("date")] public DateTime Date { get; set; }
    [MapColumn("patient_id"), ForeignKey("Patient")] public int PatientId { get; set; }
    public Patient Patient { get; set; }
    [MapColumn("patient2_id"), ForeignKey("Patient2")] public int Patient2Id { get; set; }
    public Patient Patient2 { get; set; }

    public Attendance()
    { }

    public static async Task<Attendance?> Get(int id)
    {
        return await ConsoleContext.Get().AttendanceSet
            .Find(id, s => s.Patient, s => s.Patient2);
    }
}