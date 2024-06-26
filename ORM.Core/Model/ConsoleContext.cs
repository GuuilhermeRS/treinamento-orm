using ORM.Console.Model.Entities;
using ORM.Core;

namespace ORM.Console.Model;

public class ConsoleContext : OrmContext
{
    private static AsyncLocal<ConsoleContext> _instance = new();
    private static readonly string ConnectionString = "server=127.0.0.1;user id=root;pwd=root;database=studies";
    public DbSet<Student> StudentSet;
    public DbSet<Patient> PatientSet;
    public DbSet<Attendance> AttendanceSet;

    private bool _loadedPatientCache = false;
    private Dictionary<int, Patient> _patientCacheByErpId = new();

    private ConsoleContext(bool startTransaction = false, bool log = false) : base(ConnectionString, startTransaction, log)
    {
    }

    public static ConsoleContext Get(bool startTransaction = false, bool log = false)
    {
        return _instance.Value ?? (_instance.Value = new ConsoleContext(startTransaction, log));
    }

    public override void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
        _instance = new();
        base.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        _instance = new AsyncLocal<ConsoleContext>();
        _loadedPatientCache = false;
        _patientCacheByErpId.Clear();
        await base.DisposeAsync();
    }
    
    private async Task LoadCache()
    {
        var list = await Patient.List();
        foreach (var p in list)
        {
            _patientCacheByErpId.Add(p.ErpId, p);
        }

        _loadedPatientCache = true;
    }
    
    public async Task<Patient?> GetPatientByErpId(int erpId)
    {
        if (!_loadedPatientCache)
        {
            await LoadCache();
        }

        return _patientCacheByErpId.GetValueOrDefault(erpId);
    }
}