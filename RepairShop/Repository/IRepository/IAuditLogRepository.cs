using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task AddAsy(AuditLog AuditLog);

        Task AddRangeAsy(IEnumerable<AuditLog> AuditLogs);

        Task UpdateAsy(AuditLog AuditLog);

        Task UpdateRangeAsy(IEnumerable<AuditLog> AuditLogs);
    }
}