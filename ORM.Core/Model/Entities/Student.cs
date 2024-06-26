using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ORM.Core.Util;

namespace ORM.Console.Model.Entities;

[Table("student")]
public class Student
{
    [Key, MapColumn("id")] public int Id { get; set; }
    [MapColumn("name")] public string Name { get; set; }
    [MapColumn("email")] public string Email { get; set; }
}