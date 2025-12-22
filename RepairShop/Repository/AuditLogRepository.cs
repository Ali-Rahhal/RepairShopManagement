using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository
{
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        private readonly AppDbContext _db;

        public AuditLogRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(AuditLog AuditLog)
        {
            await _db.AuditLogs.AddAsync(AuditLog);

            await _db.SaveChangesAsync();

            AuditLog.Code = AuditLog.Id.ToString();

            await UpdateAsy(AuditLog);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<AuditLog> AuditLogs)
        {
            await _db.AuditLogs.AddRangeAsync(AuditLogs);
            await _db.SaveChangesAsync();
            foreach (var AuditLog in AuditLogs)
            {
                AuditLog.Code = AuditLog.Id.ToString();
            }

            await UpdateRangeAsy(AuditLogs);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(AuditLog AuditLog)
        {
            _db.AuditLogs.Update(AuditLog);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<AuditLog> AuditLogs)
        {
            _db.AuditLogs.UpdateRange(AuditLogs);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(AuditLog AuditLog)
        {
            AuditLog.IsActive = false;
            await UpdateAsy(AuditLog);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<AuditLog> AuditLogs)
        {
            foreach (var item in AuditLogs)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}